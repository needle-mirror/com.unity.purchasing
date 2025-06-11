#nullable enable

namespace UnityEngine.Purchasing
{
    internal interface INativeStoreProvider
    {
        INativeAppleStore GetStorekit(IUnityCallback callback);
    }
}
