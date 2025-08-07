
#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    // **_Note to the Unity IAP development team: Only use this class in forward-facing code (Ex: test apps, samples, legacy wrappers), not in any internal package code. Unit tests must not fully instantiate the full set of IAP objects_**
    // End users and developers of other Unity packages can freely use this, as that is the intention.

    /// <summary>
    /// Unity IAP Services Entry Point
    /// </summary>
    public static class UnityIAPServices
    {
        /// <summary>
        /// A service responsible for connecting to the default store and its extensions.
        /// </summary>
        /// <returns>The default IStoreService implementation for the current default platform.</returns>
        public static IStoreService DefaultStore() { return StoreServiceProvider.GetDefaultStoreService(); }

        /// <summary>
        /// A service responsible for connecting to a store and its extensions.
        /// </summary>
        /// <param name="storeName"> The name of the store for which to get the service. </param>
        /// <returns>The IStoreService implementation for the specified store.</returns>
        public static IStoreService Store(string storeName) { return StoreServiceProvider.GetStoreService(storeName); }

        /// <summary>
        /// A service responsible for ordering products, fetching previous purchases from the default store and validating product entitlements.
        /// </summary>
        /// <returns>The default IPurchaseService implementation for the current default platform.</returns>
        public static IPurchaseService DefaultPurchase() { return PurchaseServiceProvider.GetDefaultPurchaseService(); }

        /// <summary>
        /// A service responsible for ordering products, fetching previous purchases from the store indicated and validating product entitlements.
        /// </summary>
        /// <param name="storeName"> The name of the store for which to get the service. </param>
        /// <returns>The IPurchaseService implementation for the specified store.</returns>
        public static IPurchaseService Purchase(string storeName) { return PurchaseServiceProvider.GetPurchaseService(storeName); }

        /// <summary>
        /// A service responsible for fetching and storing products available for purchase from the default store.
        /// </summary>
        /// <returns>The default IProductService implementation for the current default platform.</returns>
        public static IProductService DefaultProduct() { return ProductServiceProvider.GetDefaultProductService(); }

        /// <summary>
        /// A service responsible for fetching and storing products available for purchase.
        /// </summary>
        /// <param name="storeName"> The name of the store for which to get the service. </param>
        /// <returns>The IProductService implementation for the specified store.</returns>
        public static IProductService Product(string storeName) { return ProductServiceProvider.GetProductService(storeName); }

        /// <summary>
        /// Used by Applications to control Unity In-App Purchasing.
        /// This is a wrapper over the services meant to be used a single point of entry for the application.
        /// </summary>
        /// <param name="storeName">The name of the store for which to get the store controller. When null, the default store will be used.</param>
        /// <returns>A new StoreController instance for the specified store, or the default store if storeName is null.</returns>
        public static StoreController StoreController(string? storeName = null)
        {
            return new StoreController(storeName);
        }

        /// <summary>
        /// Sets a different store as the default. Useful for using custom stores.
        /// </summary>
        /// <param name="storeName"> The name of the store and therefore services to be referenced by default by internal and external calls. </param>
        public static void SetStoreAsDefault(string storeName)
        {
            DefaultStoreHelper.OverrideDefaultStoreName(storeName);
        }

        /// <summary>
        /// Add a new custom store which can then be referenced by name to get its services. This will register the store with IAP.
        /// If no custom services are added via the other functions, a generic StoreService, ProductService and PurchaseService will be created to drive the custom store with no extended features.
        /// </summary>
        /// <param name="customStoreWrapper"> The implementation of the store wrapper for the custom store,</param>
        public static void AddNewCustomStore(IStoreWrapper customStoreWrapper)
        {
            StoreManager.Instance().AddNewCustomStore(customStoreWrapper);
        }

        /// <summary>
        /// Add a function for a new custom StoreService, the result of which will be retained and maintained by the API. This function assumes that all of the interface of IStoreService has been implemented from scratch.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createStoreService"> A function to return an instance of the Store Service. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewStoreService(string storeName, Func<IStoreService> createStoreService)
        {
            StoreServiceFactoryManager.Instance().GetServiceFactory().RegisterNewService(storeName, createStoreService);
        }

        /// <summary>
        /// Add a function for a new custom StoreService, the result of which will be retained and maintained by the API.
        /// This function assumes that the interface of IStoreService will be the SDK's generic once and that extension and configuration implementations will be added to the derived version of  <c>ExtensibleStoreService</c>.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createStoreService"> A function to return an instance of the custom <c>ExtensibleStoreService</c>. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewExtendedStoreService(string storeName, Func<IStoreService, ExtensibleStoreService> createStoreService)
        {
            StoreServiceFactoryManager.Instance().GetServiceFactory().RegisterNewExtendedService(storeName, createStoreService);
        }

        /// <summary>
        /// Add a function for a new custom Product Service, the result of which will be retained and maintained by the API. This function assumes that all of the interface of IProductService has been implemented from scratch.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createProductService"> A function to return an instance of the Product Service. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewProductService(string storeName, Func<IProductService> createProductService)
        {
            ProductServiceFactoryManager.Instance().GetServiceFactory().RegisterNewService(storeName, createProductService);
        }

        /// <summary>
        /// Add a function for a new custom Product Service, the result of which will be retained and maintained by the API.
        /// This function assumes that the interface of IProductService will be the SDK's generic once and that extension and configuration implementations will be added to the derived version of  <c>ExtensibleProductService</c>.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createProductService"> A function to return an instance of the custom <c>ExtensibleProductService</c>. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewExtendedProductService(string storeName, Func<IProductService, ExtensibleProductService> createProductService)
        {
            ProductServiceFactoryManager.Instance().GetServiceFactory().RegisterNewExtendedService(storeName, createProductService);
        }

        /// <summary>
        /// Add a function for a new custom Purchase Service, the result of which will be retained and maintained by the API. This function assumes that all of the interface of IPurchaseService has been implemented from scratch.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createPurchaseService"> A function to return an instance of the Purchase Service. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewPurchaseService(string storeName, Func<IPurchaseService> createPurchaseService)
        {
            PurchaseServiceFactoryManager.Instance().GetServiceFactory().RegisterNewService(storeName, createPurchaseService);
        }

        /// <summary>
        /// Add a function for a new custom Purchase Service, the result of which will be retained and maintained by the API.
        /// This function assumes that the interface of IPurchaseService will be the SDK's generic once and that extension and configuration implementations will be added to the derived version of  <c>ExtensiblePurchaseService</c>.
        /// </summary>
        /// <param name="storeName"> The name of the store for which the custom service is mapped. </param>
        /// <param name="createPurchaseService"> A function to return an instance of the custom <c>ExtensiblePurchaseService</c>. Normally this will be called only once upon first reference and calling Store(storeName) will always return the same instance. </param>
        public static void AddNewExtendedPurchaseService(string storeName, Func<IPurchaseService, ExtensiblePurchaseService> createPurchaseService)
        {
            PurchaseServiceFactoryManager.Instance().GetServiceFactory().RegisterNewExtendedService(storeName, createPurchaseService);
        }
    }
}
