using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.LiveContentAdapterService.Apis.Client;
using UnityEngine.Purchasing.LiveContentAdapterService.Client;
using UnityEngine.Purchasing.LiveContentAdapterService.Http;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal class InternalLiveContentAdapterService : ILiveContentAdapterService
    {
        readonly ILiveContentAdapterServiceExceptionMapper m_ServiceExceptionMapper;
        readonly IEnvironmentId m_EnvironmentId;
        readonly ICloudProjectId m_CloudProjectId;
        readonly IClientApiClient m_ApiClient;
        readonly Configuration m_Configuration;

        internal InternalLiveContentAdapterService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null)
        {
            var url = baseUrl ?? "https://services.api.unity.com/live-content/client/v1";
            m_ApiClient = new ClientApiClient(new HttpClient(), accessToken);
            m_Configuration = new Configuration(url, 10, 4, null);

            m_EnvironmentId = environmentId;
            m_CloudProjectId = cloudProjectId;

            m_ServiceExceptionMapper = new LiveContentAdapterServiceExceptionMapper();
        }

        public async Task<List<ConfigContentData>> GetConfigsContent(string schema = null, string schemaVersion = null, int? limit = null, string after = null)
        {
            CheckForCloudProjectInfo();

            var request = new GetPlayerConfigsContentRequest(
                projectId: m_CloudProjectId.GetCloudProjectId(),
                schema: schema,
                schemaVersion: schemaVersion,
                limit: limit,
                after: after
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await m_ApiClient.GetPlayerConfigsContentAsync(request, m_Configuration);
                return CreateConfigContentDataFromResponse(response.Result);
            });
        }

        void CheckForCloudProjectInfo()
        {
            if (m_EnvironmentId?.EnvironmentId is null ||
                m_CloudProjectId?.GetCloudProjectId() is null)
            {
                throw new CloudProjectAuthenticationException();
            }
        }

        static List<ConfigContentData> CreateConfigContentDataFromResponse(List<Models.ClientFileWithContent> results)
        {
            if (results == null || results.Count == 0)
            {
                return new List<ConfigContentData>();
            }

            try
            {
                return results.Select(r => new ConfigContentData
                {
                    id = r.Id,
                    path = r.Path,
                    contentHash = r.ContentHash,
                    contentSize = r.ContentSize,
                    content = JsonConvert.SerializeObject(r.Content),
                    schemas = r.Schemas,
                    variantTag = r.VariantTag
                }).ToList();
            }
            catch (Exception e)
            {
                throw new ResponseDeserializationException($"Error deserializing ConfigContentData from response: {e.Message}");
            }
        }
    }
}
