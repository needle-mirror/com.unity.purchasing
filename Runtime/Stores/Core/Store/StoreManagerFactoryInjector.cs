namespace UnityEngine.Purchasing
{
    static class StoreManagerFactoryInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SetStoreManagerFactory()
        {
            IStoreManagerFactoryInjectionPoint injectionPoint = StoreManager.Instance();
            injectionPoint.SetStoreFactory(StoreFactory.Instance());
        }
    }
}
