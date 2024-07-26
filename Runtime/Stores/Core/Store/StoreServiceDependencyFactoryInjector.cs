namespace UnityEngine.Purchasing
{
    static class StoreServiceDependencyFactoryInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SetStoreManagerFactory()
        {
            IStoreServiceFactoryManagerInjectionPoint injectionPoint = StoreServiceFactoryManager.Instance();
            injectionPoint.SetServiceFactory(StoreServiceFactory.Instance());
        }
    }
}
