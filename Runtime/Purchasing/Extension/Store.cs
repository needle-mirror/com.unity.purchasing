#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Extension point for purchasing plugins.
    ///
    /// An abstract class is provided so that methods can be added to the IStore
    /// without breaking binary compatibility with existing plugins.
    /// </summary>

    public abstract class Store : IStore
    {
        protected IStoreProductsCallback? ProductsCallback;
        protected IStorePurchaseFetchCallback? PurchaseFetchCallback;
        protected IStorePurchaseCallback? PurchaseCallback;
        protected IStorePurchaseConfirmCallback? ConfirmCallback;
        protected IStoreCheckEntitlementCallback? EntitlementCallback;
        protected IStoreConnectCallback? ConnectCallback;
        protected IOnEntitlementRevokedCallback? EntitlementRevokedCallback;
        internal IProductCache ProductCache;

        protected Store()
        {
            ProductCache = new ProductCache();
        }

        public abstract void Connect();
        public abstract void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products);
        public abstract void FetchPurchases();
        public abstract void Purchase(ICart cart);

        public abstract void FinishTransaction(PendingOrder pendingOrder);

        public abstract void CheckEntitlement(ProductDefinition product);

        public virtual void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchPurchaseCallback)
        {
            PurchaseFetchCallback = fetchPurchaseCallback;
        }

        public virtual void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback)
        {
            PurchaseCallback = purchaseCallback;
        }

        public virtual void SetPurchaseConfirmCallback(IStorePurchaseConfirmCallback confirmCallback)
        {
            ConfirmCallback = confirmCallback;
        }

        public virtual void SetStoreConnectionCallback(IStoreConnectCallback storeConnectCallback)
        {
            ConnectCallback = storeConnectCallback;
        }

        public virtual void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            ProductsCallback = productsCallback;
        }

        public virtual void SetEntitlementCheckCallback(IStoreCheckEntitlementCallback entitlementCallback)
        {
            EntitlementCallback = entitlementCallback;
        }

        public virtual void SetOnRevokedEntitlementCallback(IOnEntitlementRevokedCallback entitlementRevokedCallback)
        {
            EntitlementRevokedCallback = entitlementRevokedCallback;
        }
    }
}
