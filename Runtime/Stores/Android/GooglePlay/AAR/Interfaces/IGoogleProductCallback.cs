#nullable enable

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleProductCallback
    {
        void SetStoreConfiguration(IGooglePlayConfigurationInternal configuration);
        void NotifyQueryProductDetailsFailed(int retryCount);
    }
}
