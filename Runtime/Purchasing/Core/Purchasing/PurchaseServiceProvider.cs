#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides access to the Purchase Service API by locating or creating an instance of an <c>IPurchaseService</c>
    /// </summary>
    static class PurchaseServiceProvider
    {
        /// <summary>
        /// Get the default <c>IPurchaseService<c> for the build platform and selected store, where applicable.
        /// </summary>
        /// <returns> The Purchase Service for the default store. </returns>
        public static IPurchaseService GetDefaultPurchaseService()
        {
            var manager = StoreManager.Instance();
            var wrapper = manager.GetDefaultStore();

            return GetPurchaseServiceInternal(wrapper);
        }

        /// <summary>
        /// Get the <c>IPurchaseService</c> for the store specified.
        /// </summary>
        /// <param name="storeName"> Name of the store for which to provide the Product Service. </param>
        /// <returns> The Purchase Service for the specified store. </returns>
        public static IPurchaseService GetPurchaseService(string storeName)
        {
            return GetPurchaseServiceInternal(StoreManager.Instance().GetStore(storeName));
        }

        static IPurchaseService GetPurchaseServiceInternal(IStoreWrapper storeWrapper)
        {
            return LocateExistingService(storeWrapper) ?? CreateNewService(storeWrapper);
        }

        static IPurchaseService? LocateExistingService(IStoreWrapper wrapper)
        {
            return PurchaseServiceContainer.Instance().FindService(wrapper.name);
        }

        static IPurchaseService CreateNewService(IStoreWrapper wrapper)
        {
            var dependencyInjector = new PurchaseServiceDependencyInjector(wrapper);
            var service = dependencyInjector.CreatePurchaseService();

            try
            {
                PurchaseServiceContainer.Instance().SetService(wrapper.name, service);
            }
            catch (ServiceCreationException exception)
            {
                Debug.unityLogger.LogIAPException(exception);
            }

            return service;
        }
    }
}
