using Unity.Services.Authentication.Internal;
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

        public void CreateLiveContentAdapterService(IAccessToken accessToken, IEnvironmentId environmentId, string baseUrl)
        {
            m_LiveContentAdapterService ??= new InternalLiveContentAdapterService(accessToken, environmentId, baseUrl);
        }
    }
}
