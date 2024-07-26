#nullable enable

using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing
{
    class StoreServiceDependencyInjector
    {
        readonly IRetryPolicy? m_RetryPolicy;
        readonly IStoreWrapper m_Store;
        IStoreServiceFactoryManager m_ServiceFactoryManager;

        internal StoreServiceDependencyInjector(IStoreWrapper storeWrapper, IRetryPolicy? retryPolicy)
        {
            m_RetryPolicy = retryPolicy;
            m_Store = storeWrapper;
            m_ServiceFactoryManager = StoreServiceFactoryManager.Instance();
        }

        internal IStoreService CreateStoreService()
        {
            return m_ServiceFactoryManager.GetServiceFactory().Create(m_Store, m_RetryPolicy);
        }
    }
}
