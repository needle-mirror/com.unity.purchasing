#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides access to the Store Service API by locating or creating an instance of an <c>IStoreService</c>
    /// </summary>
    static class StoreServiceProvider
    {
        /// <summary>
        /// Get the default <c>IStoreService</c> for the build platform and selected store, where applicable.
        /// </summary>
        /// <returns> The Store Service for the default store. </returns>
        public static IStoreService GetDefaultStoreService()
        {
            var manager = StoreManager.Instance();
            var wrapper = manager.GetDefaultStore();

            return GetStoreServiceInternal(wrapper);
        }

        /// <summary>
        /// Get the <c>IStoreService</c> for the store specified.
        /// </summary>
        /// <param name="storeName"> The name of the store for which to obtain the service </param>
        /// <returns> The Store Service for the specified store. </returns>
        public static IStoreService GetStoreService(string storeName)
        {
            return GetStoreServiceInternal(StoreManager.Instance().GetStore(storeName));
        }

        static IStoreService GetStoreServiceInternal(IStoreWrapper storeWrapper)
        {
            return LocateExistingService(storeWrapper) ?? CreateNewService(storeWrapper);
        }

        static IStoreService? LocateExistingService(IStoreWrapper wrapper)
        {
            return StoreServiceContainer.Instance().FindService(wrapper.name);
        }

        static IStoreService CreateNewService(IStoreWrapper wrapper)
        {
            var dependencyInjector = new StoreServiceDependencyInjector(wrapper, new ExponentialBackOffRetryPolicy());
            var service = dependencyInjector.CreateStoreService();

            try
            {
                StoreServiceContainer.Instance().SetService(wrapper.name, service);
            }
            catch (ServiceCreationException exception)
            {
                Debug.unityLogger.LogIAPException(exception);
            }

            return service;
        }
    }
}
