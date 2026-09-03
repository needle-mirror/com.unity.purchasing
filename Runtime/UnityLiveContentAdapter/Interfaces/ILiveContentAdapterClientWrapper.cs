using Unity.Services.Authentication.Internal;
using UnityEngine.Purchasing.LiveContentAdapterService;

namespace UnityEngine.Purchasing.Stores
{
    internal interface ILiveContentAdapterClientWrapper
    {
        bool LiveContentAdapterClientIsAvailable { get; }
        ILiveContentAdapterService GetLiveContentAdapterService();
        void CreateLiveContentAdapterService(IAccessToken accessToken, IEnvironmentId environmentId, string baseUrl);
    }
}
