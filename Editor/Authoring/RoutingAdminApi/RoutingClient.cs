using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Purchasing.Editor.Shared.Clients;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Services.Core.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.RoutingAdminApi
{
    class RoutingClient : IRoutingClient
    {
        const string k_TargetingPath =
            "/projects/{projectId}/environments/{environmentId}/targeting";

        readonly IApiClient m_ApiClient;
        readonly ApiConfiguration m_Configuration;
        readonly IAccessTokens m_TokenProvider;

        string m_ProjectId;
        string m_EnvironmentId;

        public RoutingClient(IApiClient apiClient, IAccessTokens tokenProvider)
        {
            m_ApiClient = apiClient;
            m_TokenProvider = tokenProvider;
            m_Configuration = new ApiConfiguration
            {
                BasePath = RoutingAdminEnvironment.BasePath
            };
        }

        public async Task Initialize(
            string environmentId,
            string projectId,
            CancellationToken cancellationToken)
        {
            await UpdateToken();
            m_EnvironmentId = environmentId;
            m_ProjectId = projectId;
        }

        public async Task<ProviderRoutingConfig> FetchRouting(CancellationToken cancellationToken)
        {
            EnsureInitialized();
            await UpdateToken();

            var response = await m_ApiClient.Get(
                k_TargetingPath,
                BuildOptions(null),
                m_Configuration,
                cancellationToken);

            ThrowIfFailed(response, nameof(FetchRouting));

            if (string.IsNullOrEmpty(response.Content))
            {
                return new ProviderRoutingConfig();
            }

            return JsonConvert.DeserializeObject<ProviderRoutingConfig>(response.Content)
                ?? new ProviderRoutingConfig();
        }

        public async Task PushRouting(ProviderRoutingConfig config, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            await UpdateToken();

            var response = await m_ApiClient.Post(
                k_TargetingPath,
                BuildOptions(config),
                m_Configuration,
                cancellationToken);

            ThrowIfFailed(response, nameof(PushRouting));
        }

        ApiRequestOptions BuildOptions(object body)
        {
            var options = new ApiRequestOptions
            {
                PathParameters = new Dictionary<string, string>
                {
                    { "projectId", m_ProjectId },
                    { "environmentId", m_EnvironmentId }
                }
            };

            if (body != null)
            {
                options.Data = body;
                options.HeaderParameters.Add("Content-Type", "application/json");
            }

            return options;
        }

        async Task UpdateToken()
        {
            var token = await m_TokenProvider.GetServicesGatewayTokenAsync();
            m_Configuration.DefaultHeaders =
                new AdminApiHeaders<RoutingClient>(token).ToDictionary();
        }

        void EnsureInitialized()
        {
            if (string.IsNullOrEmpty(m_ProjectId) || string.IsNullOrEmpty(m_EnvironmentId))
            {
                throw new InvalidOperationException(
                    $"{nameof(RoutingClient)} not initialized. Call Initialize first.");
            }
        }

        static void ThrowIfFailed(ApiResponse response, string operation)
        {
            if (response.IsSuccessful)
            {
                return;
            }

            var body = string.IsNullOrEmpty(response.Content) ? "(empty body)" : response.Content;
            throw new ClientException(
                $"{operation} failed with HTTP {response.StatusCode}: {body}",
                null);
        }
    }
}
