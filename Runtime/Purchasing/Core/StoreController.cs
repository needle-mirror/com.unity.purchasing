#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class StoreController : IStoreService, IProductService, IPurchaseService
    {
        IStoreService m_StoreService;
        IProductService m_ProductService;
        IPurchaseService m_PurchaseService;

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
        IAppleStoreExtendedProductService? IProductService.Apple => m_ProductService.Apple;
        IAppleStoreExtendedPurchaseService? IPurchaseService.Apple => m_PurchaseService.Apple;
        IGooglePlayStoreExtendedPurchaseService? IPurchaseService.Google => m_PurchaseService.Google;

        public IAppleStoreExtendedService? AppleStoreExtendedStoreService => m_StoreService.Apple;
        public IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService => m_StoreService.Google;
        public IAppleStoreExtendedProductService? AppleStoreExtendedProductService => m_ProductService.Apple;
        public IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService => m_PurchaseService.Apple;
        public IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService => m_PurchaseService.Google;
        #endregion

        #region Callbacks
        public event Action<StoreConnectionFailureDescription>? OnStoreDisconnected
        {
            add => m_StoreService.OnStoreDisconnected += value;
            remove => m_StoreService.OnStoreDisconnected -= value;
        }

        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            m_PurchaseService.ProcessPendingOrdersOnPurchasesFetched(shouldProcess);
        }

        public event Action<PendingOrder>? OnPurchasePending
        {
            add => m_PurchaseService.OnPurchasePending += value;
            remove => m_PurchaseService.OnPurchasePending -= value;
        }

        public event Action<Order>? OnPurchaseConfirmed
        {
            add => m_PurchaseService.OnPurchaseConfirmed += value;
            remove => m_PurchaseService.OnPurchaseConfirmed -= value;
        }

        public event Action<FailedOrder>? OnPurchaseFailed
        {
            add => m_PurchaseService.OnPurchaseFailed += value;
            remove => m_PurchaseService.OnPurchaseFailed -= value;
        }

        public event Action<DeferredOrder>? OnPurchaseDeferred
        {
            add => m_PurchaseService.OnPurchaseDeferred += value;
            remove => m_PurchaseService.OnPurchaseDeferred -= value;
        }

        public event Action<Orders>? OnPurchasesFetched
        {
            add => m_PurchaseService.OnPurchasesFetched += value;
            remove => m_PurchaseService.OnPurchasesFetched -= value;
        }

        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
        {
            add => m_PurchaseService.OnPurchasesFetchFailed += value;
            remove => m_PurchaseService.OnPurchasesFetchFailed -= value;
        }

        public event Action<Entitlement>? OnCheckEntitlement
        {
            add => m_PurchaseService.OnCheckEntitlement += value;
            remove => m_PurchaseService.OnCheckEntitlement -= value;
        }

        public event Action<List<Product>>? OnProductsFetched
        {
            add => m_ProductService.OnProductsFetched += value;
            remove => m_ProductService.OnProductsFetched -= value;
        }

        public event Action<ProductFetchFailed>? OnProductsFetchFailed
        {
            add => m_ProductService.OnProductsFetchFailed += value;
            remove => m_ProductService.OnProductsFetchFailed -= value;
        }
        #endregion

        #region StoreService
        public Task Connect() => m_StoreService.Connect();
        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy) => m_StoreService.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy);
        #endregion

        #region ProductService
        public void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions) => m_ProductService.FetchProductsWithNoRetries(productDefinitions);
        public void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy? retryPolicy = null) => m_ProductService.FetchProducts(productDefinitions, retryPolicy);
        public ReadOnlyObservableCollection<Product> GetProducts() => m_ProductService.GetProducts();
        public Product? GetProductById(string productId) => m_ProductService.GetProductById(productId);
        #endregion

        #region PurchaseService
        public void PurchaseProduct(Product product) => m_PurchaseService.PurchaseProduct(product);
        public void Purchase(ICart cart) => m_PurchaseService.Purchase(cart);
        public void ConfirmPurchase(PendingOrder order) => m_PurchaseService.ConfirmPurchase(order);
        public void FetchPurchases() => m_PurchaseService.FetchPurchases();
        public void CheckEntitlement(Product product) => m_PurchaseService.CheckEntitlement(product);
        public void RestoreTransactions(Action<bool, string?>? callback) => m_PurchaseService.RestoreTransactions(callback);
        public ReadOnlyObservableCollection<Order> GetPurchases() => m_PurchaseService.GetPurchases();
        #endregion

        #region IStoreController
        // TODO: IAP-3929 - Move to IStoreController
        /// <summary>
        /// Purchase a product by product id
        /// Be sure to first register callbacks via `AddPurchasePendingAction` and `AddPurchaseFailedAction`.
        /// </summary>
        /// <param name="productId">The product id to purchase.</param>
        public void PurchaseProduct(string? productId)
        {
            var products = m_ProductService.GetProducts();
            var product = products?.FirstOrDefault(product => product.definition.id == productId) ?? Product.CreateUnknownProduct(productId);
            m_PurchaseService.PurchaseProduct(product);
        }
        #endregion
    }
}
