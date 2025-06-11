#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides access to the Product Service API by locating or creating an instance of an <c>IProductService<c>
    /// </summary>
    static class ProductServiceProvider
    {
        /// <summary>
        /// Get the default <c>IProductService<c> for the build platform and selected store, where applicable.
        /// </summary>
        /// <returns> The Product Service for the default store </returns>
        public static IProductService GetDefaultProductService()
        {
            var manager = StoreManager.Instance();
            var wrapper = manager.GetDefaultStore();

            return GetProductServiceInternal(wrapper);
        }

        /// <summary>
        /// Get the <c>IProductService<c> for the store specified.
        /// </summary>
        /// <param name="storeName"> Name of the store for which to provide the Product Service. </param>
        /// <returns> The Product Service for the specified store. </returns>
        public static IProductService GetProductService(string storeName)
        {
            return GetProductServiceInternal(StoreManager.Instance().GetStore(storeName));
        }

        static IProductService GetProductServiceInternal(IStoreWrapper storeWrapper)
        {
            return LocateExistingService(storeWrapper) ?? CreateNewService(storeWrapper);
        }

        static IProductService? LocateExistingService(IStoreWrapper wrapper)
        {
            return ProductServiceContainer.Instance().FindService(wrapper.name ?? string.Empty);
        }

        static IProductService CreateNewService(IStoreWrapper wrapper)
        {
            var dependencyInjector = new ProductServiceDependencyInjector(wrapper);
            var service = dependencyInjector.CreateProductService();

            try
            {
                ProductServiceContainer.Instance().SetService(wrapper.name ?? string.Empty, service);
            }
            catch (ServiceCreationException exception)
            {
                Debug.unityLogger.LogIAPException(exception);
            }

            return service;
        }
    }
}
