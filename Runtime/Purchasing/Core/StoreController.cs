
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Main controller class that provides a unified interface for store operations, product management, and purchase handling.
    /// </summary>
    public class StoreController : IStoreService, IProductService, IPurchaseService
    {
        IStoreService m_StoreService;
        IProductService m_ProductService;
        IPurchaseService m_PurchaseService;

        /// <summary>
        /// Initializes a new instance of the StoreController class.
        /// </summary>
        /// <param name="storeName">Optional store name to use for service selection. If null, default services will be used.</param>
        public StoreController(string? storeName = null)
        {
            m_StoreService = storeName != null ? StoreServiceProvider.GetStoreService(storeName) : StoreServiceProvider.GetDefaultStoreService();
            m_ProductService = storeName != null ? ProductServiceProvider.GetProductService(storeName) : ProductServiceProvider.GetDefaultProductService();
            m_PurchaseService = storeName != null ? PurchaseServiceProvider.GetPurchaseService(storeName) : PurchaseServiceProvider.GetDefaultPurchaseService();
        }

        internal void SetTestInstances(IStoreService storeService, IProductService productService, IPurchaseService purchaseService)
        {
            m_StoreService = storeService;
            m_ProductService = productService;
            m_PurchaseService = purchaseService;
        }

#region ExtendedServices
        IAppleStoreExtendedService? IStoreService.Apple => m_StoreService.Apple;
        IGooglePlayStoreExtendedService? IStoreService.Google => m_StoreService.Google;
        IPaymentProvidersExtendedService? IStoreService.PaymentProviders => m_StoreService.PaymentProviders;
        IAppleStoreExtendedProductService? IProductService.Apple => m_ProductService.Apple;
        IAppleStoreExtendedPurchaseService? IPurchaseService.Apple => m_PurchaseService.Apple;
        IGooglePlayStoreExtendedPurchaseService? IPurchaseService.Google => m_PurchaseService.Google;
        IPaymentProvidersExtendedPurchaseService? IPurchaseService.PaymentProviders => m_PurchaseService.PaymentProviders;

        /// <summary>
        /// Gets the Apple Store extended store service for platform-specific operations.
        /// </summary>
        public IAppleStoreExtendedService? AppleStoreExtendedService => m_StoreService.Apple;

        /// <summary>
        /// Gets the Google Play Store extended service for platform-specific operations.
        /// </summary>
        public IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService => m_StoreService.Google;

        /// <summary>
        /// Gets the Payment Provider Store extended service for platform-specific operations.
        /// </summary>
        public IPaymentProvidersExtendedService? PaymentProviderStoreExtendedService => m_StoreService.PaymentProviders;

        /// <summary>
        /// Gets the Apple Store extended product service for platform-specific product operations.
        /// </summary>
        public IAppleStoreExtendedProductService? AppleStoreExtendedProductService => m_ProductService.Apple;

        /// <summary>
        /// Gets the Apple Store extended purchase service for platform-specific purchase operations.
        /// </summary>
        public IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService => m_PurchaseService.Apple;

        /// <summary>
        /// Gets the Google Play Store extended purchase service for platform-specific purchase operations.
        /// </summary>
        public IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService => m_PurchaseService.Google;

        /// <summary>
        /// Gets the Payment Providers extended purchase service for platform-specific purchase operations.
        /// </summary>
        public IPaymentProvidersExtendedPurchaseService? PaymentProvidersExtendedPurchaseService => m_PurchaseService.PaymentProviders;
#endregion

#region Callbacks
        /// <summary>
        /// Event triggered when the store connection is lost or fails.
        /// </summary>
        public event Action<StoreConnectionFailureDescription>? OnStoreDisconnected
        {
            add => m_StoreService.OnStoreDisconnected += value;
            remove => m_StoreService.OnStoreDisconnected -= value;
        }

        /// <summary>
        /// Event triggered when the store connection is successfully established.
        /// </summary>
        public event Action? OnStoreConnected
        {
            add => m_StoreService.OnStoreConnected += value;
            remove => m_StoreService.OnStoreConnected -= value;
        }

        /// <summary>
        /// Event triggered after the authenticated end-user account has changed and the
        /// store's product and purchase caches have been cleared.
        /// </summary>
        public event Action? OnAuthAccountChanged
        {
            add => m_StoreService.OnAuthAccountChanged += value;
            remove => m_StoreService.OnAuthAccountChanged -= value;
        }

        /// <summary>
        /// Configures whether pending orders should be automatically processed when purchases are fetched.
        /// </summary>
        /// <param name="shouldProcess">True to automatically process pending orders, false otherwise.</param>
        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            m_PurchaseService.ProcessPendingOrdersOnPurchasesFetched(shouldProcess);
        }

        /// <summary>
        /// Event triggered when a purchase is pending and awaiting user action or store processing.
        /// </summary>
        public event Action<PendingOrder>? OnPurchasePending
        {
            add => m_PurchaseService.OnPurchasePending += value;
            remove => m_PurchaseService.OnPurchasePending -= value;
        }

        /// <summary>
        /// Event triggered when confirming a purchase with either a ConfirmedOrder or a FailedOrder.
        /// </summary>
        public event Action<Order>? OnPurchaseConfirmed
        {
            add => m_PurchaseService.OnPurchaseConfirmed += value;
            remove => m_PurchaseService.OnPurchaseConfirmed -= value;
        }

        /// <summary>
        /// Event triggered when a purchase attempt has failed.
        /// </summary>
        public event Action<FailedOrder>? OnPurchaseFailed
        {
            add => m_PurchaseService.OnPurchaseFailed += value;
            remove => m_PurchaseService.OnPurchaseFailed -= value;
        }

        /// <summary>
        /// Event triggered when a purchase has been deferred by the store.
        /// This typically occurs when parental approval is required for the purchase.
        /// </summary>
        public event Action<DeferredOrder>? OnPurchaseDeferred
        {
            add => m_PurchaseService.OnPurchaseDeferred += value;
            remove => m_PurchaseService.OnPurchaseDeferred -= value;
        }

        /// <summary>
        /// Event triggered when existing purchases have been successfully fetched from the store.
        /// </summary>
        public event Action<Orders>? OnPurchasesFetched
        {
            add => m_PurchaseService.OnPurchasesFetched += value;
            remove => m_PurchaseService.OnPurchasesFetched -= value;
        }

        /// <summary>
        /// Event triggered when fetching existing purchases from the store has failed.
        /// </summary>
        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
        {
            add => m_PurchaseService.OnPurchasesFetchFailed += value;
            remove => m_PurchaseService.OnPurchasesFetchFailed -= value;
        }

        /// <summary>
        /// Event triggered when an entitlement check operation completes.
        /// </summary>
        public event Action<Entitlement>? OnCheckEntitlement
        {
            add => m_PurchaseService.OnCheckEntitlement += value;
            remove => m_PurchaseService.OnCheckEntitlement -= value;
        }

        /// <summary>
        /// Gets a specific product by its catalog listing id.
        /// </summary>
        /// <param name="catalogListingId">The catalog listing id to search for.</param>
        /// <returns>The product if found, or null if not found.</returns>
        public Product? GetProductByCatalogListingId(string catalogListingId) => m_ProductService.GetProductByCatalogListingId(catalogListingId);

        /// <summary>
        /// Event triggered when products have been successfully fetched from the store.
        /// </summary>
        public event Action<List<Product>>? OnProductsFetched
        {
            add => m_ProductService.OnProductsFetched += value;
            remove => m_ProductService.OnProductsFetched -= value;
        }

        /// <summary>
        /// Event triggered when fetching products from the store has failed.
        /// </summary>
        public event Action<ProductFetchFailed>? OnProductsFetchFailed
        {
            add => m_ProductService.OnProductsFetchFailed += value;
            remove => m_ProductService.OnProductsFetchFailed -= value;
        }
#endregion

#region StoreService
        /// <summary>
        /// Establishes a connection to the store asynchronously.
        /// </summary>
        /// <returns>A task representing the connection operation.</returns>
        public Task Connect() => m_StoreService.Connect();

        /// <summary>
        /// Sets the retry policy to use when the store disconnects and needs to reconnect.
        /// </summary>
        /// <param name="retryPolicy">The retry policy to use, or null to disable automatic reconnection.</param>
        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy) => m_StoreService.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy);

        /// <summary>
        /// Gets the current connection state of the store.
        /// </summary>
        /// <returns>The state of the connection to the store.</returns>
        public ConnectionState GetConnectionState() => m_StoreService.GetConnectionState();
#endregion

#region ProductService
        /// <summary>
        /// Fetches product information from the store without retry logic.
        /// </summary>
        /// <param name="productDefinitions">The list of product definitions to fetch information for.</param>
        public void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions) => m_ProductService.FetchProductsWithNoRetries(productDefinitions);

        /// <summary>
        /// Fetches product information from the store with optional retry policy.
        /// </summary>
        /// <param name="productDefinitions">The list of product definitions to fetch information for.</param>
        /// <param name="retryPolicy">Optional retry policy to use for failed requests.</param>
        public void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy? retryPolicy = null) => m_ProductService.FetchProducts(productDefinitions, retryPolicy);

        /// <summary>
        /// Gets a read-only collection of all currently loaded products.
        /// </summary>
        /// <returns>A read-only observable collection of products.</returns>
        public ReadOnlyObservableCollection<Product> GetProducts() => m_ProductService.GetProducts();

        /// <summary>
        /// Gets a specific product by its catalog listing id.
        /// </summary>
        /// <param name="catalogListingId">The catalog listing id to search for.</param>
        /// <returns>The product if found, or null if not found.</returns>
        public Product? GetProductById(string catalogListingId) => m_ProductService.GetProductById(catalogListingId);
#endregion

#region PurchaseService
        /// <summary>
        /// Initiates a purchase for the specified product.
        /// Be sure to first register callbacks via `OnPurchasePending` and `OnPurchaseFailed`.
        /// </summary>
        /// <param name="product">The product to purchase.</param>
        public void PurchaseProduct(Product product) => m_PurchaseService.PurchaseProduct(product);

        /// <summary>
        /// Initiates a purchase for all items in the specified cart.
        /// Be sure to first register callbacks via `OnPurchasePending` and `OnPurchaseFailed`.
        /// </summary>
        /// <param name="cart">The cart containing items to purchase.</param>
        public void Purchase(ICart cart) => m_PurchaseService.Purchase(cart);

        /// <summary>
        /// Confirms a pending purchase order, completing the transaction.
        /// Be sure to first register callbacks via `OnPurchaseConfirmed`.
        /// </summary>
        /// <param name="order">The pending order to confirm.</param>
        public void ConfirmPurchase(PendingOrder order) => m_PurchaseService.ConfirmPurchase(order);

        /// <summary>
        /// Fetches existing purchases from the store.
        /// Be sure to first register callbacks via `OnPurchasesFetched` and `OnPurchasesFetchFailed`.
        /// </summary>
        public void FetchPurchases() => m_PurchaseService.FetchPurchases();

        /// <summary>
        /// Checks the entitlement status for the specified product.
        /// Be sure to first register callbacks via `OnCheckEntitlement`.
        /// </summary>
        /// <param name="product">The product to check entitlement for.</param>
        public void CheckEntitlement(Product product) => m_PurchaseService.CheckEntitlement(product);

        /// <summary>
        /// Restores previously purchased transactions (iOS and some other platforms).
        /// </summary>
        /// <param name="callback">Optional callback to handle the restore operation result.</param>
        public void RestoreTransactions(Action<bool, string?>? callback) => m_PurchaseService.RestoreTransactions(callback);

        /// <summary>
        /// Gets a read-only collection of all completed purchase orders.
        /// </summary>
        /// <returns>A read-only observable collection of orders.</returns>
        public ReadOnlyObservableCollection<Order> GetPurchases() => m_PurchaseService.GetPurchases();
#endregion

#region IStoreController
        // TODO: IAP-3929 - Move to IStoreController
        /// <summary>
        /// Purchase a product by its catalog listing id.
        /// Be sure to first register callbacks via `OnPurchasePending` and `OnPurchaseFailed`.
        /// </summary>
        /// <param name="catalogListingId">The catalog listing id of the product to purchase.</param>
        public void PurchaseProduct(string catalogListingId)
        {
            m_PurchaseService.PurchaseProduct(catalogListingId);
        }
#endregion
    }
}
