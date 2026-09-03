using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Services.Core.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    class EditorLiveContentApiTransport : ILiveContentApiTransport
    {
        readonly IAccessTokens m_TokenProvider;
        readonly IConfigsApi m_ConfigsApi;
        string m_EnvironmentId;
        string m_ProjectId;

        public EditorLiveContentApiTransport(IAccessTokens tokenProvider, IConfigsApi configsApi)
        {
            m_TokenProvider = tokenProvider;
            m_ConfigsApi = configsApi;
        }

        public async Task InitializeAsync(string environmentId, string projectId, CancellationToken cancellationToken)
        {
            m_EnvironmentId = environmentId;
            m_ProjectId = projectId;
            await RefreshTokenAsync();
        }

        async Task RefreshTokenAsync()
        {
            await LiveContentAdminApiHeaderConfigurator.UpdateAuthenticationHeaders<EditorLiveContentApiTransport>(
                m_ConfigsApi, m_TokenProvider.GetServicesGatewayTokenAsync);
        }

        public async Task<TransportResult> GetConfigPathsAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
            bool noVariantTag,
            CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.GetConfigs(
                m_EnvironmentId,
                m_ProjectId,
                path: pathPrefix,
                limit: limit,
                after: after,
                start: start,
                schema: schema,
                noVariantTag: noVariantTag,
                cancellationToken: cancellationToken);

            InlineVariantFormat.LogErrorIfDetected(r.Content);

            return ToResult(r);
        }

        public async Task<TransportResult> GetConfigContentAsync(string path, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.GetConfigContent(
                m_EnvironmentId,
                m_ProjectId,
                path,
                cancellationToken: cancellationToken);

            return ToResult(r);
        }

        public async Task<TransportResult> CreateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var body = IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(jsonContent);
            var r = await m_ConfigsApi.CreateConfigFile(
                m_EnvironmentId,
                m_ProjectId,
                path,
                body,
                cancellationToken: cancellationToken);

            InlineVariantFormat.LogErrorIfDetected(r.Content);

            return ToResult(r);
        }

        public async Task<TransportResult> UpdateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var body = IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(jsonContent);
            var r = await m_ConfigsApi.UpdateConfigFile(
                m_EnvironmentId,
                m_ProjectId,
                path,
                body,
                cancellationToken: cancellationToken);

            InlineVariantFormat.LogErrorIfDetected(r.Content);

            return ToResult(r);
        }

        public async Task<TransportResult> DeleteConfigAsync(string path, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.DeleteConfig(
                m_EnvironmentId,
                m_ProjectId,
                path,
                cancellationToken: cancellationToken);
            return ToResult(r);
        }

        static TransportResult ToResult(ApiResponse r) =>
            new TransportResult(r.StatusCode, r.Content, r.Headers);
    }
}
