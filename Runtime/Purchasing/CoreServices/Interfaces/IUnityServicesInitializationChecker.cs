using Unity.Services.Core;

namespace UnityEngine.Purchasing
{
    interface IUnityServicesInitializationChecker
    {
        void CheckAndLogWarning();
    }
}
