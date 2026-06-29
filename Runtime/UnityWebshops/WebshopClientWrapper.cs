using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.WebshopService;

namespace UnityEngine.Purchasing.Stores
{
    internal class WebshopClientWrapper : IWebshopClientWrapper
    {
        IWebshopService m_WebshopService;
        public bool WebshopClientIsAvailable => m_WebshopService != null;

        public IWebshopService GetWebshopService()
        {
            return m_WebshopService;
        }

        public void CreateWebshopService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null)
        {
            m_WebshopService ??= new InternalWebshopService(accessToken, environmentId, cloudProjectId, baseUrl);
        }
    }
}
