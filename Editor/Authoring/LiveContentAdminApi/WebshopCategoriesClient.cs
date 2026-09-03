using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Services.Core.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    class WebshopCategoriesClient : IWebshopCategoriesClient
    {
        internal const string Path = "webshop/categories.json";
        const string k_RequiredSchemaPath = "/v1/schemas/UnityWebshopCategories/versions/1.1.0";

        readonly IAccessTokens m_TokenProvider;
        readonly IConfigsApi m_ConfigsApi;
        readonly string m_RequiredSchema;
        string m_EnvironmentId;
        string m_ProjectId;

        public WebshopCategoriesClient(IAccessTokens tokenProvider, IConfigsApi configsApi)
            : this(tokenProvider, configsApi, LiveContentAdminEnvironment.SchemaRegistryBasePath)
        {
        }

        internal WebshopCategoriesClient(
            IAccessTokens tokenProvider,
            IConfigsApi configsApi,
            string schemaRegistryBasePath)
        {
            m_TokenProvider = tokenProvider;
            m_ConfigsApi = configsApi;
            m_RequiredSchema = schemaRegistryBasePath + k_RequiredSchemaPath;
        }

        public async Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
        {
            await UpdateToken();
            m_EnvironmentId = environmentId;
            m_ProjectId = projectId;
        }

        public async Task<WebshopCategories> Get(CancellationToken cancellationToken)
        {
            await UpdateToken();
            try
            {
                var response = await m_ConfigsApi.GetConfigContent(
                    m_EnvironmentId, m_ProjectId, Path, cancellationToken: cancellationToken);

                if (response.StatusCode == 404)
                    return null;
                if (!response.IsSuccessful)
                    throw GetRequestException(response);
                if (string.IsNullOrEmpty(response.Content))
                    return new WebshopCategories();

                return IsolatedJsonConvert.DeserializeObject<WebshopCategories>(response.Content)
                    ?? new WebshopCategories();
            }
            catch (ApiException e)
            {
                throw GetRequestException(e);
            }
        }

        public async Task Upsert(WebshopCategories categories, CancellationToken cancellationToken)
        {
            if (categories is null)
                throw new ArgumentNullException(nameof(categories));

            await UpdateToken();

            var body = SerializeBody(categories);
            try
            {
                // Live Content rejects PUT on a non-existent path; on 404 fall back to POST.
                var response = await m_ConfigsApi.UpdateConfigFile(
                    m_EnvironmentId, m_ProjectId, Path, body, cancellationToken: cancellationToken);

                InlineVariantFormat.LogErrorIfDetected(response.Content);

                if (response.StatusCode == 404)
                {
                    response = await m_ConfigsApi.CreateConfigFile(
                        m_EnvironmentId, m_ProjectId, Path, body, cancellationToken: cancellationToken);

                    InlineVariantFormat.LogErrorIfDetected(response.Content);
                }

                if (!response.IsSuccessful)
                    throw GetRequestException(response);
            }
            catch (ApiException e)
            {
                throw GetRequestException(e);
            }
        }

        // Schema must be attached on every write: the storefront's schema-filtered read hides files
        // whose $schema attachment was dropped, and PUT replaces the whole record.
        Dictionary<string, ApiObject> SerializeBody(WebshopCategories categories)
        {
            var envelope = new BodyEnvelope
            {
                Schemas = new List<string> { m_RequiredSchema },
                Categories = categories.Categories,
            };
            var json = IsolatedJsonConvert.SerializeObject(envelope,
                new JsonSerializerSettings { Formatting = Formatting.Indented });
            return IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(json);
        }

        async Task UpdateToken()
        {
            await LiveContentAdminApiHeaderConfigurator.UpdateAuthenticationHeaders<WebshopCategoriesClient>(
                m_ConfigsApi, m_TokenProvider.GetServicesGatewayTokenAsync);
        }

        static ClientException GetRequestException(ApiException e, [CallerMemberName] string caller = null)
        {
            return new ClientException(
                $"Request '{caller} - {e.Response.Url}' failed with '{e.Response.StatusCode}'. {e.Message}", e);
        }

        static ClientException GetRequestException(ApiResponse response, [CallerMemberName] string caller = null)
        {
            return new ClientException(
                $"Request '{caller} - {response.Url}' failed with '{response.StatusCode}'. {response.Content}", null);
        }

        class BodyEnvelope
        {
            [JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Schemas;

            [JsonProperty("categories")]
            public List<WebshopCategory> Categories;
        }
    }
}
