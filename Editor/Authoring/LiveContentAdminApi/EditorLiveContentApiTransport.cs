using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Services.Core.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
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

        public async Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
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
                variantTag: DefaultVariantTag(),
                noVariantTag: true,
                cancellationToken: cancellationToken);

            return ToResult(r, data => (IReadOnlyList<LiveContentConfig>)(
                data?.SelectMany(config => config.ToLiveContentConfigs()).ToList()
                 ?? new List<LiveContentConfig>()));
        }

        public async Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsContentAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
            CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.GetConfigsContent(
                m_EnvironmentId,
                m_ProjectId,
                limit: limit,
                path: pathPrefix,
                schema: schema,
                after: after,
                start: start,
                variantTag: DefaultVariantTag(),
                noVariantTag: true,
                cancellationToken: cancellationToken);

            return ToResult(r, data => (IReadOnlyList<LiveContentConfig>)(
                data?.SelectMany(config => config.ToLiveContentConfigs()).ToList()
                ?? new List<LiveContentConfig>()));
        }

        public async Task<TransportResult<LiveContentConfigBody>> GetConfigContentAsync(
            string path,
            CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.GetConfigContent(
                m_EnvironmentId,
                m_ProjectId,
                path,
                variantTag: DefaultVariantTag(),
                cancellationToken: cancellationToken);

            return ToResult(r, data => data?.ToLiveContentContent());
        }

        public async Task<TransportResult<LiveContentConfig>> CreateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var body = IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(jsonContent);
            var r = await m_ConfigsApi.CreateConfig(
                m_EnvironmentId,
                m_ProjectId,
                path,
                body,
                variantTag: DefaultVariantTag(),
                cancellationToken: cancellationToken);

            return ToResult(r, ToDefaultConfig);
        }

        public async Task<TransportResult<LiveContentConfig>> UpdateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var body = IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(jsonContent);
            var r = await m_ConfigsApi.UpdateConfig(
                m_EnvironmentId,
                m_ProjectId,
                path,
                body,
                variantTag: DefaultVariantTag(),
                cancellationToken: cancellationToken);

            return ToResult(r, ToDefaultConfig);
        }

        public async Task<TransportResult> DeleteConfigAsync(string path, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync();
            var r = await m_ConfigsApi.DeleteConfig(
                m_EnvironmentId,
                m_ProjectId,
                path,
                variantTag: DefaultVariantTag(),
                cancellationToken: cancellationToken);

            var success = IsSuccess(r.StatusCode);
            return new TransportResult(r.StatusCode, success ? null : ErrorTextOf(r), r.Headers);
        }

        // Maps a typed ApiResponse into a TransportResult: on success the payload is projected
        // through `map(Data)`; on failure the payload is dropped and the error body is preserved.
        static TransportResult<TDomain> ToResult<TApi, TDomain>(ApiResponse<TApi> r, Func<TApi, TDomain> map)
        {
            var success = IsSuccess(r.StatusCode);
            return new TransportResult<TDomain>(
                statusCode: r.StatusCode,
                content: success ? map(r.Data) : default,
                headers: r.Headers,
                error: success ? null : ErrorTextOf(r));
        }

        static bool IsSuccess(int statusCode) => statusCode is >= 200 and < 300;

        static string ErrorTextOf(ApiResponse r) =>
            string.IsNullOrEmpty(r.Content) ? r.ErrorText : r.Content;

        static List<string> DefaultVariantTag() => new() { string.Empty };

        // The API can include multiple variants in write responses; expose only the tagless default.
        static LiveContentConfig ToDefaultConfig(ConfigMetadata data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var configs = data.ToLiveContentConfigs();
            var matchingConfigs = configs
                .Where(config => config.VariantTags.Count == 0)
                .ToList();

            if (matchingConfigs.Count != 1)
                throw new InvalidOperationException(
                    $"Expected one tagless default config response variant, but received {matchingConfigs.Count} matches.");

            return matchingConfigs[0];
        }
    }
}
