using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.LiveContentAdapterService;

namespace UnityEngine.Purchasing.Stores
{
    internal interface ILiveContentAdapterClientWrapper
    {
        bool LiveContentAdapterClientIsAvailable { get; }
        ILiveContentAdapterService GetLiveContentAdapterService();
        void CreateLiveContentAdapterService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null);
    }
}
