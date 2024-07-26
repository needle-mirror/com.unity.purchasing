#nullable enable

namespace UnityEngine.Purchasing
{
    internal class ProductServiceDependencyInjector
    {
        IStoreWrapper m_storeWrapper;
        IProductServiceFactoryManager m_ServiceFactoryManager;

        internal ProductServiceDependencyInjector(IStoreWrapper storeWrapper)
        {
            m_ServiceFactoryManager = ProductServiceFactoryManager.Instance();
            m_storeWrapper = storeWrapper;
        }

        internal IProductService CreateProductService()
        {
            return m_ServiceFactoryManager.GetServiceFactory().Create(m_storeWrapper);
        }
    }
}
