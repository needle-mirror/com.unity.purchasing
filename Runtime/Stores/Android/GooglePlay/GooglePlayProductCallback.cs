#nullable enable

using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class GooglePlayProductCallback : IGoogleProductCallback
    {
        IGooglePlayConfigurationInternal? m_GooglePlayConfigurationInternal;

        public void SetStoreConfiguration(IGooglePlayConfigurationInternal configuration)
        {
            m_GooglePlayConfigurationInternal = configuration;
        }

        public void NotifyQueryProductDetailsFailed(int retryCount)
        {
            m_GooglePlayConfigurationInternal?.NotifyQueryProductDetailsFailed(retryCount);
        }
    }
}
