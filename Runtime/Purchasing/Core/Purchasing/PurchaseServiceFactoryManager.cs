namespace UnityEngine.Purchasing
{
    class PurchaseServiceFactoryManager : IPurchaseServiceFactoryManager, IPurchaseServiceFactoryManagerInjectionPoint
    {
        static PurchaseServiceFactoryManager s_Instance;
        IPurchaseServiceFactory m_Factory;

        internal static PurchaseServiceFactoryManager Instance()
        {
            if (s_Instance == null)
            {
                s_Instance = new PurchaseServiceFactoryManager();
            }

            return s_Instance;
        }

        public void SetServiceFactory(IPurchaseServiceFactory serviceFactory)
        {
            m_Factory = serviceFactory;
        }

        public IPurchaseServiceFactory GetServiceFactory()
        {
            return m_Factory;
        }
    }
}
