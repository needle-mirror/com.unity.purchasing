#nullable enable

using System;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Services;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The service responsible for ordering products, fetching previous purchases and validating product entitlements.
    /// </summary>
    public class PurchaseService : IPurchaseService
    {
        readonly IFetchPurchasesUseCase m_FetchPurchasesUseCase;
        readonly IPurchaseUseCase m_PurchaseUseCase;
        readonly IConfirmOrderUseCase m_ConfirmOrderUseCase;
        readonly ICheckEntitlementUseCase m_CheckEntitlementUseCase;
        internal readonly ObservableCollection<Order> m_Purchases = new ObservableCollection<Order>();
        readonly ReadOnlyObservableCollection<Order> m_PurchasesReadOnly;
        readonly IStoreWrapper m_StoreWrapper;
        readonly IAnalyticsClient m_AnalyticsClient;

        event Action<PendingOrder>? onPendingOrderUpdated;
        event Action<ConfirmedOrder>? onConfirmedOrderUpdated;
        event Action<FailedOrder>? onPurchaseFailed;
        event Action<DeferredOrder>? onDeferredOrderUpdated;
        event Action<Orders>? onPurchasesFetched;
        event Action<PurchasesFetchFailureDescription>? onFetchFailed;
        event Action<Entitlement>? onEntitlementChecked;

        internal PurchaseService(IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient
        )
        {
            m_FetchPurchasesUseCase = fetchPurchasesUseCase;
            m_PurchaseUseCase = purchaseUseCase;
            m_ConfirmOrderUseCase = confirmOrderUseCase;
            m_CheckEntitlementUseCase = checkEntitlementUseCase;
            m_PurchasesReadOnly = new ReadOnlyObservableCollection<Order>(m_Purchases);

            purchaseUseCase.OnPurchaseSuccess += OnPurchaseSucceeded;
            purchaseUseCase.OnPurchaseFail += OnPurchaseFailed;
            purchaseUseCase.OnPurchaseDefer += OnPurchaseDeferred;
            m_StoreWrapper = storeWrapper;
            m_AnalyticsClient = analyticsClient;
        }

        public IAppleStoreExtendedPurchaseService? Apple => this as IAppleStoreExtendedPurchaseService;

        public IGooglePlayStoreExtendedPurchaseService? Google => this as IGooglePlayStoreExtendedPurchaseService;

        /// <summary>
        /// Purchase a product. Can run asynchronously.
        /// Be sure to first register callbacks via `AddPendingOrderUpdatedAction` and `AddPurchaseFailedAction`.
        /// </summary>
        /// <param name="product">The Product to purchase.</param>
        public void PurchaseProduct(Product product)
        {
            CheckStoreConnectionState();
            var cart = new Cart(product);
            Purchase(cart);
        }

        /// <summary>
        /// Purchase a cart. Can run asynchronously.
        /// Be sure to first register callbacks via `AddPendingOrderUpdatedAction` and `AddPurchaseFailedAction`.
        /// </summary>
        /// <param name="cart">The cart to purchase.</param>
        public void Purchase(ICart cart)
        {
            CheckStoreConnectionState();
            if (onPendingOrderUpdated == null)
            {
                throw new PurchaseException("No Pending Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddPendingOrderUpdatedAction()`.");
            }

            if (onPurchaseFailed == null)
            {
                throw new PurchaseException("No Failed Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddPurchaseFailedAction()`.");
            }

            m_PurchaseUseCase.Purchase(cart);
        }

        void OnPurchaseSucceeded(PendingOrder order)
        {
            m_Purchases.Add(order);
            onPendingOrderUpdated?.Invoke(order);
        }

        void OnPurchaseFailed(FailedOrder order)
        {
            m_AnalyticsClient.OnPurchaseFailed(order);
            onPurchaseFailed?.Invoke(order);
        }

        void OnPurchaseDeferred(DeferredOrder order)
        {
            onDeferredOrderUpdated?.Invoke(order);
        }

        /// <summary>
        /// Confirm a pending order. Can run asynchronously.
        /// Be sure to first register callbacks via `AddConfirmedOrderUpdatedAction` and `AddPurchaseFailedAction`.
        /// </summary>
        /// <param name="order">The pending order to confirm.</param>
        public void ConfirmOrder(PendingOrder order)
        {
            CheckStoreConnectionState();
            if (onConfirmedOrderUpdated == null)
            {
                throw new ConfirmOrderException("No Confirm Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddConfirmedOrderUpdatedAction()`.");
            }

            if (onPurchaseFailed == null)
            {
                throw new ConfirmOrderException("No Failed Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddPurchaseFailedAction()`.");
            }

            m_ConfirmOrderUseCase.ConfirmOrder(order, OnConfirmSucceeded, OnConfirmFailed);
        }

        void OnConfirmSucceeded(PendingOrder pendingOrder, ConfirmedOrder confirmedOrder)
        {
            m_Purchases.Remove(pendingOrder);
            m_Purchases.Add(confirmedOrder);

            m_AnalyticsClient.OnPurchaseSucceeded(confirmedOrder);
            onConfirmedOrderUpdated?.Invoke(confirmedOrder);
        }

        void OnConfirmFailed(PendingOrder pendingOrder, FailedOrder failedOrder)
        {
            onPurchaseFailed?.Invoke(failedOrder);
        }

        /// <summary>
        /// Fetch pre-existing purchases. Can run asynchronously.
        /// Be sure to first register callbacks via `AddFetchedPurchasesAction` and `AddFetchPurchasesFailedAction`.
        /// </summary>
        public void FetchPurchases()
        {
            CheckStoreConnectionState();
            if (onPurchasesFetched == null)
            {
                throw new PurchaseFetchException("No Pending Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddFetchedPurchasesAction()`.");
            }

            if (onFetchFailed == null)
            {
                throw new PurchaseFetchException("No Confirm Purchase Updated actions set. No success callbacks will be sent for this call. Please set an action via `AddFetchPurchasesFailedAction()`.");
            }

            m_FetchPurchasesUseCase.FetchPurchases(OnFetchSuccess, OnFetchFailure);
        }

        void OnFetchSuccess(Orders fetchedPurchases)
        {
            foreach (var fetchedPurchase in fetchedPurchases.ConfirmedOrders)
            {
                m_Purchases.Add(fetchedPurchase);
            }

            foreach (var fetchedPurchase in fetchedPurchases.PendingOrders)
            {
                m_Purchases.Add(fetchedPurchase);
            }

            onPurchasesFetched?.Invoke(fetchedPurchases);
        }

        void OnFetchFailure(PurchasesFetchFailureDescription fetchFailed)
        {
            onFetchFailed?.Invoke(fetchFailed);
        }

        /// <summary>
        /// Check if a Product has been entitled.
        /// </summary>
        /// <param name="product">The Product to check for entitlements.</param>
        public void IsProductEntitled(Product product)
        {
            CheckStoreConnectionState();
            if (onEntitlementChecked == null)
            {
                throw new CheckEntitlementException("No CheckEntitlement actions set. No callbacks will be sent for this call. Please set an action via `AddCheckEntitlementAction()`.");
            }

            m_CheckEntitlementUseCase.IsProductEntitled(product, OnEntitlementChecked);
        }

        public void RestoreTransactions(Action<bool, string?> callback)
        {
            RestoreTransactionsInternal(callback);
        }

        protected virtual void RestoreTransactionsInternal(Action<bool, string?> callback)
        {
            Debug.LogWarning(Application.platform + " is not a supported platform for the restore button");
        }

        void OnEntitlementChecked(Entitlement entitlement)
        {
            onEntitlementChecked?.Invoke(entitlement);
        }

        /// <summary>
        /// Gets an observable collection of the purchases fetched, confirmed and ordered.
        /// </summary>
        /// <returns>The read-only observable collection of all purchases made.</returns>
        public ReadOnlyObservableCollection<Order> GetPurchases()
        {
            CheckStoreConnectionState();
            return m_PurchasesReadOnly;
        }

        /// <summary>
        /// Add an action to be called when products are ordered.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        public void AddPendingOrderUpdatedAction(Action<PendingOrder>? updatedAction)
        {
            onPendingOrderUpdated -= updatedAction;
            onPendingOrderUpdated += updatedAction;
        }

        /// <summary>
        /// Add an action to be called when product orders are confirmed.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        public void AddConfirmedOrderUpdatedAction(Action<ConfirmedOrder>? updatedAction)
        {
            onConfirmedOrderUpdated -= updatedAction;
            onConfirmedOrderUpdated += updatedAction;
        }

        /// <summary>
        /// Add an action to be called when products orders fail.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        public void AddPurchaseFailedAction(Action<FailedOrder>? failedAction)
        {
            onPurchaseFailed -= failedAction;
            onPurchaseFailed += failedAction;
        }

        /// <summary>
        /// Add an action to be called when products orders are deferred.
        /// </summary>
        /// <param name="deferredAction">The action to be added to the list of callbacks.</param>
        public void AddPurchaseDeferredAction(Action<DeferredOrder>? deferredAction)
        {
            onDeferredOrderUpdated -= deferredAction;
            onDeferredOrderUpdated += deferredAction;
        }

        /// <summary>
        /// Add an action to be called when previous purchases are fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        public void AddFetchedPurchasesAction(Action<Orders>? updatedAction)
        {
            onPurchasesFetched -= updatedAction;
            onPurchasesFetched += updatedAction;
        }

        /// <summary>
        /// Add an action to be called when an attempt to fetch previous purchases has failed.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        public void AddFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription>? failedAction)
        {
            onFetchFailed -= failedAction;
            onFetchFailed += failedAction;
        }

        /// <summary>
        /// Add an action to be called when a check for product entitlement is complete.
        /// </summary>
        /// <param name="checkEntitlementAction">The action to be added to the list of callbacks.</param>
        public void AddCheckEntitlementAction(Action<Entitlement>? checkEntitlementAction)
        {
            onEntitlementChecked -= checkEntitlementAction;
            onEntitlementChecked += checkEntitlementAction;
        }

        /// <summary>
        /// Remove an action to be called when products are ordered.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        public void RemovePendingOrderUpdatedAction(Action<PendingOrder>? updatedAction)
        {
            onPendingOrderUpdated -= updatedAction;
        }

        /// <summary>
        /// Remove an action to be called when product orders are confirmed.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        public void RemoveConfirmedOrderUpdatedAction(Action<ConfirmedOrder>? updatedAction)
        {
            onConfirmedOrderUpdated -= updatedAction;
        }

        /// <summary>
        /// Remove an action to be called when products orders fail.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        public void RemovePurchaseFailedAction(Action<FailedOrder>? failedAction)
        {
            onPurchaseFailed -= failedAction;
        }

        /// <summary>
        /// Remove an action to be called when previous purchases are fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        public void RemoveFetchedPurchasesAction(Action<Orders>? updatedAction)
        {
            onPurchasesFetched -= updatedAction;
        }

        /// <summary>
        /// Remove an action to be called when an attempt to fetch previous purchases has failed.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        public void RemoveFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription>? failedAction)
        {
            onFetchFailed -= failedAction;
        }

        /// <summary>
        /// Remove an action to be called when a check for product entitlement is complete.
        /// </summary>
        /// <param name="checkEntitlementAction">The action to be added to the list of callbacks.</param>
        public void RemoveCheckEntitlementAction(Action<Entitlement>? checkEntitlementAction)
        {
            onEntitlementChecked -= checkEntitlementAction;
        }

        void CheckStoreConnectionState()
        {
            if (m_StoreWrapper.GetStoreConnectionState() != ConnectionState.Connected)
            {
                throw new StoreConnectionException(
                    "Purchase Service couldn't execute its task, store connection state: " + m_StoreWrapper.GetStoreConnectionState()
                );
            }
        }
    }
}
