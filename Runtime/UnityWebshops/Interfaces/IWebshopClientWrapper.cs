using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.WebshopService;

namespace UnityEngine.Purchasing.Stores
{
    internal interface IWebshopClientWrapper
    {
        bool WebshopClientIsAvailable { get; }
        IWebshopService GetWebshopService();
        void CreateWebshopService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null);
    }
}
