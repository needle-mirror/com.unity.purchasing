#nullable enable

using System;
using System.Collections.Generic;
#if IAP_UNITY_AUTH_ENABLED
using Unity.Services.Authentication;
using Unity.Services.Core;
#endif

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
        /// <summary>
        /// Callback for handling products response from the store.
        /// </summary>
        protected IStoreProductsCallback? ProductsCallback;

        /// <summary>
        /// Callback for handling purchase fetch operations from the store.
        /// </summary>
        protected IStorePurchaseFetchCallback? PurchaseFetchCallback;

        /// <summary>
        /// Callback for handling purchase operations from the store.
        /// </summary>
        protected IStorePurchaseCallback? PurchaseCallback;

        /// <summary>
        /// Callback for handling purchase confirmation operations from the store.
        /// </summary>
        protected IStorePurchaseConfirmCallback? ConfirmCallback;

        /// <summary>
        /// Callback for handling entitlement check operations from the store.
        /// </summary>
        protected IStoreCheckEntitlementCallback? EntitlementCallback;

        /// <summary>
        /// Callback for handling store connection operations.
        /// </summary>
        protected IStoreConnectCallback? ConnectCallback;

        /// <summary>
        /// Callback for handling revoked entitlement notifications from the store.
        /// </summary>
        protected IOnEntitlementRevokedCallback? EntitlementRevokedCallback;

        internal IProductCache ProductCache;
        internal IPurchaseCache PurchaseCache;

        /// <summary>
        /// Read-only cache of products specific to the store.
        /// The cache is populated using responses passed to <see cref="IStoreProductsCallback.OnProductsFetched"/>.
        /// </summary>
        protected IReadOnlyProductCache ReadOnlyProductCache => ProductCache;

        /// <summary>
        /// Initializes a new instance of the Store class.
        /// </summary>
        protected Store()
        {
            ProductCache = new ProductCache();
            PurchaseCache = new PurchaseCache();
        }

        Action? m_OnAuthAccountChanged;

        // Internal event raised after Unity Authentication reports a PlayerId change for
        // this store. The public surface for subscribers is IStoreService.OnAuthAccountChanged
        // (forwarded by StoreController / StoreService / ExtensibleStoreService /
        // StoreConnectUseCase).
        internal event Action? OnAuthAccountChanged
        {
            add
            {
                m_OnAuthAccountChanged += value;
#if IAP_UNITY_AUTH_ENABLED
                TryWireAuth();
#endif
            }
            remove
            {
                m_OnAuthAccountChanged -= value;
#if IAP_UNITY_AUTH_ENABLED
                if (m_OnAuthAccountChanged == null)
                {
                    TearDownAuthWiring();
                }
#endif
            }
        }

        // Internal trigger for OnAuthAccountChanged. Fired by the SDK itself when Unity
        // Authentication reports a PlayerId change. No-op when no subscriber is present —
        // caches are only cleared when at least one handler is listening.
        internal void NotifyAuthAccountChanged()
        {
            if (m_OnAuthAccountChanged == null)
            {
                return;
            }
            ProductCache.Clear();
            PurchaseCache.Clear();
            m_OnAuthAccountChanged.Invoke();
        }

        internal bool HasAuthAccountChangedSubscriber => m_OnAuthAccountChanged != null;

#if IAP_UNITY_AUTH_ENABLED
        string? m_LastKnownPlayerId;
        bool m_AuthWired;
        bool m_AwaitingUnityServicesInit;

        void TryWireAuth()
        {
            if (m_AuthWired)
            {
                return;
            }

            ServicesInitializationState state;
            try
            {
                state = UnityServices.State;
            }
            catch
            {
                return;
            }

            if (state == ServicesInitializationState.Initialized)
            {
                WireAuth();
                return;
            }

            if (!m_AwaitingUnityServicesInit)
            {
                UnityServices.Initialized += OnUnityServicesInitialized;
                m_AwaitingUnityServicesInit = true;
            }
        }

        void OnUnityServicesInitialized()
        {
            WireAuth();
        }

        void WireAuth()
        {
            if (m_AuthWired)
            {
                return;
            }
            try
            {
                m_LastKnownPlayerId = AuthenticationService.Instance.IsSignedIn
                    ? AuthenticationService.Instance.PlayerId
                    : null;
                AuthenticationService.Instance.SignedIn += OnUnityAuthSignedIn;
                m_AuthWired = true;
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"OnAuthAccountChanged: failed to wire AuthenticationService: {e.Message}");
            }
        }

        void TearDownAuthWiring()
        {
            if (m_AwaitingUnityServicesInit)
            {
                try
                {
                    UnityServices.Initialized -= OnUnityServicesInitialized;
                }
                catch (Exception)
                {
                    // Services already torn down. Nothing to do.
                }
                m_AwaitingUnityServicesInit = false;
            }
            if (m_AuthWired)
            {
                try
                {
                    AuthenticationService.Instance.SignedIn -= OnUnityAuthSignedIn;
                }
                catch (Exception)
                {
                    // Auth already torn down (e.g. during shutdown). Nothing to do.
                }
                m_AuthWired = false;
            }
            m_LastKnownPlayerId = null;
        }

        void OnUnityAuthSignedIn()
        {
            var currentId = AuthenticationService.Instance.PlayerId;
            if (currentId == null)
            {
                return;
            }
            // First sign-in of the session — including cold-start sign-in to the cached account —
            // is not an account *change*, so just record the id without firing.
            if (m_LastKnownPlayerId == null)
            {
                m_LastKnownPlayerId = currentId;
                return;
            }
            if (currentId != m_LastKnownPlayerId)
            {
                m_LastKnownPlayerId = currentId;
                NotifyAuthAccountChanged();
            }
        }
#endif

        /// <summary>
        /// Establishes a connection to the store.
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Fetches product information from the store for the specified products.
        /// </summary>
        /// <param name="products">The collection of product definitions to fetch information for.</param>
        public abstract void FetchProducts(IReadOnlyCollection<ProductDefinition> products);

        /// <summary>
        /// Fetches existing purchases from the store.
        /// </summary>
        public abstract void FetchPurchases();

        /// <summary>
        /// Initiates a purchase transaction for the items in the specified cart.
        /// </summary>
        /// <param name="cart">The cart containing items to purchase.</param>
        public abstract void Purchase(ICart cart);

        /// <summary>
        /// Completes a pending purchase transaction.
        /// </summary>
        /// <param name="pendingOrder">The pending order to finish.</param>
        public abstract void FinishTransaction(PendingOrder pendingOrder);

        /// <summary>
        /// Checks the entitlement status for the specified product.
        /// </summary>
        /// <param name="product">The product definition to check entitlement for.</param>
        public abstract void CheckEntitlement(ProductDefinition product);

        /// <summary>
        /// Sets the callback for purchase fetch operations.
        /// </summary>
        /// <param name="fetchPurchaseCallback">The callback to handle purchase fetch responses.</param>
        public virtual void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchPurchaseCallback)
        {
            PurchaseFetchCallback = fetchPurchaseCallback;
        }

        /// <summary>
        /// Sets the callback for purchase operations.
        /// </summary>
        /// <param name="purchaseCallback">The callback to handle purchase responses.</param>
        public virtual void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback)
        {
            PurchaseCallback = purchaseCallback;
        }

        /// <summary>
        /// Sets the callback for purchase confirmation operations.
        /// </summary>
        /// <param name="confirmCallback">The callback to handle purchase confirmation responses.</param>
        public virtual void SetPurchaseConfirmCallback(IStorePurchaseConfirmCallback confirmCallback)
        {
            ConfirmCallback = confirmCallback;
        }

        /// <summary>
        /// Sets the callback for store connection operations.
        /// </summary>
        /// <param name="storeConnectCallback">The callback to handle store connection responses.</param>
        public virtual void SetStoreConnectionCallback(IStoreConnectCallback storeConnectCallback)
        {
            ConnectCallback = storeConnectCallback;
        }

        /// <summary>
        /// Sets the callback for products operations.
        /// </summary>
        /// <param name="productsCallback">The callback to handle products responses.</param>
        public virtual void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            ProductsCallback = productsCallback;
        }

        /// <summary>
        /// Sets the callback for entitlement check operations.
        /// </summary>
        /// <param name="entitlementCallback">The callback to handle entitlement check responses.</param>
        public virtual void SetEntitlementCheckCallback(IStoreCheckEntitlementCallback entitlementCallback)
        {
            EntitlementCallback = entitlementCallback;
        }

        /// <summary>
        /// Sets the callback for revoked entitlement notifications.
        /// </summary>
        /// <param name="entitlementRevokedCallback">The callback to handle revoked entitlement notifications.</param>
        public virtual void SetOnRevokedEntitlementCallback(IOnEntitlementRevokedCallback entitlementRevokedCallback)
        {
            EntitlementRevokedCallback = entitlementRevokedCallback;
        }
    }
}
