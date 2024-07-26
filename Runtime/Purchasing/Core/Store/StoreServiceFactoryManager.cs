namespace UnityEngine.Purchasing
{
    internal class StoreServiceFactoryManager : IStoreServiceFactoryManager, IStoreServiceFactoryManagerInjectionPoint
    {
        static StoreServiceFactoryManager s_Instance;
        IStoreServiceFactory m_Factory;

        internal static StoreServiceFactoryManager Instance()
        {
            if (s_Instance == null)
            {
                s_Instance = new StoreServiceFactoryManager();
            }

            return s_Instance;
        }

        public void SetServiceFactory(IStoreServiceFactory serviceFactory)
        {
            m_Factory = serviceFactory;
        }

        public IStoreServiceFactory GetServiceFactory()
        {
            return m_Factory;
        }
    }
}
