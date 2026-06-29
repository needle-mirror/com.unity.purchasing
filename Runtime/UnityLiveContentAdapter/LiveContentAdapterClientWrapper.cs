using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.LiveContentAdapterService;

namespace UnityEngine.Purchasing.Stores
{
    internal class LiveContentAdapterClientWrapper : ILiveContentAdapterClientWrapper
    {
        ILiveContentAdapterService m_LiveContentAdapterService;
        public bool LiveContentAdapterClientIsAvailable => m_LiveContentAdapterService != null;

        public ILiveContentAdapterService GetLiveContentAdapterService()
        {
            return m_LiveContentAdapterService;
        }

        public void CreateLiveContentAdapterService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null)
        {
            m_LiveContentAdapterService ??= new InternalLiveContentAdapterService(accessToken, environmentId, cloudProjectId, baseUrl);
        }
    }
}
