namespace UnityEngine.Purchasing
{
    internal class ProductServiceFactoryManager : IProductServiceFactoryManager, IProductServiceFactoryManagerInjectionPoint
    {
        static ProductServiceFactoryManager s_Instance;
        IProductServiceFactory m_Factory;

        internal static ProductServiceFactoryManager Instance()
        {
            if (s_Instance == null)
            {
                s_Instance = new ProductServiceFactoryManager();
            }

            return s_Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_Instance = null;
        }

        public void SetServiceFactory(IProductServiceFactory serviceFactory)
        {
            m_Factory = serviceFactory;
        }

        public IProductServiceFactory GetServiceFactory()
        {
            return m_Factory;
        }
    }
}
