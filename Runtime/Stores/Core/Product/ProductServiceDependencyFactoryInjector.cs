namespace UnityEngine.Purchasing
{
    static class ProductServiceDependencyFactoryInjector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SetStoreManagerFactory()
        {
            IProductServiceFactoryManagerInjectionPoint injectionPoint = ProductServiceFactoryManager.Instance();
            injectionPoint.SetServiceFactory(ProductServiceFactory.Instance());
        }
    }
}
