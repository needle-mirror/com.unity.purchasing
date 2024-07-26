namespace UnityEngine.Purchasing
{
    static class PurchaseServiceDependencyFactoryInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SetStoreManagerFactory()
        {
            IPurchaseServiceFactoryManagerInjectionPoint injectionPoint = PurchaseServiceFactoryManager.Instance();
            injectionPoint.SetServiceFactory(PurchaseServiceFactory.Instance());
        }
    }
}
