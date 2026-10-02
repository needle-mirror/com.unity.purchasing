using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    class WebshopCategoriesClient : IWebshopCategoriesClient
    {
        internal const string Path = "webshop/categories.json";
        const string k_RequiredSchemaPath = "/v1/schemas/UnityWebshopCategories/versions/1.1.0";

        readonly ILiveContentApiTransport m_Transport;
        readonly string m_RequiredSchema;

        public WebshopCategoriesClient(ILiveContentApiTransport transport)
            : this(transport, LiveContentAdminEnvironment.SchemaRegistryBasePath) { }

        internal WebshopCategoriesClient(
            ILiveContentApiTransport transport,
            string schemaRegistryBasePath)
        {
            m_Transport = transport;
            m_RequiredSchema = schemaRegistryBasePath + k_RequiredSchemaPath;
        }

        public Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
        {
            return m_Transport.InitializeAsync(environmentId, projectId, cancellationToken);
        }

        public async Task<WebshopCategories> Get(CancellationToken cancellationToken)
        {
            try
            {
                var response = await m_Transport.GetConfigContentAsync(Path, cancellationToken);

                if (response.StatusCode == 404)
                    return null;
                if (!response.IsSuccess)
                    throw GetRequestException(response);
                if (response.Content == null)
                    return new WebshopCategories();

                return response.Content.TryGetContentAs<WebshopCategories>(out var categories)
                    ? categories
                    : new WebshopCategories();
            }
            catch (Exception e)
            {
                throw GetRequestException(e);
            }
        }

        public async Task Upsert(WebshopCategories categories, CancellationToken cancellationToken)
        {
            if (categories is null)
                throw new ArgumentNullException(nameof(categories));

            var body = SerializeBody(categories);
            try
            {
                // Live Content rejects PUT on a non-existent path; on 404 fall back to POST.
                var response = await m_Transport.UpdateConfigAsync(Path, body, cancellationToken);

                if (response.StatusCode == 404)
                {
                    response = await m_Transport.CreateConfigAsync(Path, body, cancellationToken);
                }

                if (!response.IsSuccess)
                    throw GetRequestException(response);
            }
            catch (Exception e)
            {
                throw GetRequestException(e);
            }
        }

        // Schema must be attached on every write: the storefront's schema-filtered read hides files
        // whose $schema attachment was dropped, and PUT replaces the whole record.
        string SerializeBody(WebshopCategories categories)
        {
            var envelope = new BodyEnvelope
            {
                Schemas = new List<string> { m_RequiredSchema },
                Metadata = LiveContentMetadata.ApplyManagedBy(null),
                Categories = categories.Categories
            };
            return JsonConvert.SerializeObject(envelope,
                new JsonSerializerSettings { Formatting = Formatting.Indented });
        }

        static ClientException GetRequestException(Exception e, [CallerMemberName] string caller = null)
        {
            return new ClientException($"Request '{caller}' failed unexpectedly. {e.Message}", e);
        }

        static ClientException GetRequestException(ITransportResult response, [CallerMemberName] string caller = null)
        {
            return new ClientException($"Request '{caller}' failed with '{response.StatusCode}'. {response.Error}", null);
        }

        class BodyEnvelope
        {
            [JsonProperty("$schema", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Schemas;

            [JsonProperty("$metadata")]
            public JObject Metadata;

            [JsonProperty("categories")]
            public List<WebshopCategory> Categories;
        }
    }
}
