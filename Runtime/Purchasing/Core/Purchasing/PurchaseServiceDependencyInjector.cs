using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class PurchaseServiceDependencyInjector
    {
        IStoreWrapper m_storeWrapper;
        IPurchaseServiceFactoryManager m_ServiceFactoryManager;

        internal PurchaseServiceDependencyInjector(IStoreWrapper storeWrapper)
        {
            m_ServiceFactoryManager = PurchaseServiceFactoryManager.Instance();
            m_storeWrapper = storeWrapper;
        }

        internal IPurchaseService CreatePurchaseService()
        {
            return m_ServiceFactoryManager.GetServiceFactory().Create(m_storeWrapper);
        }
    }
}
