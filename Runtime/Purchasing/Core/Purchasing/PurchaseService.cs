#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
#if IAP_TX_VERIFIER_ENABLED
using UnityEngine.Purchasing.TransactionVerifier;
using UnityEngine.Purchasing.TransactionVerifier.Http;
#endif

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
        internal readonly ObservableCollection<Order> m_Purchases = new();
        readonly ReadOnlyObservableCollection<Order> m_PurchasesReadOnly;
        readonly IStoreWrapper m_StoreWrapper;
        readonly IAnalyticsClient m_AnalyticsClient;

#if IAP_TX_VERIFIER_ENABLED
            readonly Store m_StoreName;
            readonly ITransactionVerifier m_TransactionVerifier;
#endif

        public event Action<PendingOrder>? OnPurchasePending;
        public event Action<Order>? OnPurchaseConfirmed;
        public event Action<FailedOrder>? OnPurchaseFailed;
        public event Action<DeferredOrder>? OnPurchaseDeferred;
        public event Action<Orders>? OnPurchasesFetched;
        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed;
        public event Action<Entitlement>? OnCheckEntitlement;

        internal PurchaseService(IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient
#if IAP_TX_VERIFIER_ENABLED
            , ITransactionVerifier transactionVerifier
#endif
        )
        {
            m_FetchPurchasesUseCase = fetchPurchasesUseCase;
            m_PurchaseUseCase = purchaseUseCase;
            m_ConfirmOrderUseCase = confirmOrderUseCase;
            m_CheckEntitlementUseCase = checkEntitlementUseCase;
            m_PurchasesReadOnly = new ReadOnlyObservableCollection<Order>(m_Purchases);

            purchaseUseCase.OnPurchaseSuccess += PurchaseSucceeded;
            purchaseUseCase.OnPurchaseFail += PurchaseFailed;
            purchaseUseCase.OnPurchaseDefer += PurchaseDeferred;
            m_StoreWrapper = storeWrapper;
            m_AnalyticsClient = analyticsClient;
#if IAP_TX_VERIFIER_ENABLED
            m_TransactionVerifier = transactionVerifier;
#endif
        }

        public IAppleStoreExtendedPurchaseService? Apple => this as IAppleStoreExtendedPurchaseService;

        public IGooglePlayStoreExtendedPurchaseService? Google => this as IGooglePlayStoreExtendedPurchaseService;

        public void PurchaseProduct(Product product)
        {
            try
            {
                var cart = new Cart(product);
                Purchase(cart);
            }
            catch (InvalidCartItemException e)
            {
                OnPurchaseFailed?.Invoke(new FailedOrder(new Cart(Product.CreateUnknownProduct("InvalidProduct")), PurchaseFailureReason.ProductUnavailable,
                    "Attempting to purchase an invalid product: " + e.Message));
            }
        }

        public void Purchase(ICart cart)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (OnPurchasePending == null)
            {
                Debug.unityLogger.LogIAPError("IPurchaseService.Purchase called without a callback defined for IPurchaseService.OnPurchasePending.");
            }

            if (OnPurchaseFailed == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.Purchase called without a callback defined for IPurchaseService.OnPurchaseFailed.");
            }

            if (OnPurchaseDeferred == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.Purchase called without a callback defined for IPurchaseService.OnPurchaseDeferred.");
            }
#endif

            if (!IsStoreConnected())
            {
                OnPurchaseFailed?.Invoke(new FailedOrder(cart, PurchaseFailureReason.StoreNotConnected,
                    "Store is not connected. Please check your internet connection and try again."));
                return;
            }

            try
            {
                m_PurchaseUseCase.Purchase(cart);
            }
            catch (Exception e)
            {
                OnPurchaseFailed?.Invoke(new FailedOrder(cart, PurchaseFailureReason.Unknown, e.Message));
            }
        }

#if IAP_TX_VERIFIER_ENABLED
        async Task HandleVerification(PendingOrder order)
        {
            try
            {
                var transactionRepresentation = m_StoreName switch
                {
                    Store.Apple => order.Info.Apple.jwsRepresentation,
                    Store.Google => order.Info.Receipt,
                    _ => null
                };

                // Verify transaction (returns response that we don't use currently)
                await m_TransactionVerifier!.VerifyPendingOrder(transactionRepresentation);
            }
            catch (Exception ex)
            {
                if (OnPurchaseFailed == null)
                {
                    Debug.unityLogger.LogWarning("Transaction Verification", "Failed to verify transaction. " + ex.Message);
                    Debug.unityLogger.LogException(ex);
                }
                else
                {
                    var failedOrder = new FailedOrder(
                        order,
                        PurchaseFailureReason.ValidationFailure,
                        "Transaction verification failed - " + ex.Message);
                    OnPurchaseFailed?.Invoke(failedOrder);
                }

                return;
            }

            OnPurchasePending?.Invoke(order);
        }
#endif

#if IAP_TX_VERIFIER_ENABLED
        async void PurchaseSucceeded(PendingOrder order)
#else
        void PurchaseSucceeded(PendingOrder order)
#endif
        {
            try
            {
                RemoveDeferredOrders(order);
                m_Purchases.Add(order);

#if IAP_TX_VERIFIER_ENABLED
                await HandleVerification(order);
#else
                OnPurchasePending?.Invoke(order);
#endif
            }
            catch (Exception ex)
            {
                Debug.unityLogger.LogException(ex);
            }
        }

        void RemoveDeferredOrders(PendingOrder pendingOrder)
        {
            var pendingOrderItems = pendingOrder.CartOrdered;
            var deferredOrders = m_Purchases.OfType<DeferredOrder>().ToList().AsReadOnly();
            var ordersToRemove = deferredOrders.Where(deferredOrder => Equals(deferredOrder.CartOrdered, pendingOrderItems)).Cast<Order>().ToList();

            foreach (var order in ordersToRemove)
            {
                m_Purchases.Remove(order);
            }
        }

        internal void PurchaseFailed(FailedOrder order)
        {
            m_AnalyticsClient.OnPurchaseFailed(order);
            OnPurchaseFailed?.Invoke(order);
        }

        void PurchaseDeferred(DeferredOrder order)
        {
            m_Purchases.Add(order);
            OnPurchaseDeferred?.Invoke(order);
        }

        public void ConfirmPurchase(PendingOrder order)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (OnPurchaseConfirmed == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.ConfirmPurchase called without a callback defined for IPurchaseService.OnPurchaseConfirmed.");
            }
#endif

            if (!IsStoreConnected())
            {
                OnConfirmFailed(new FailedOrder(
                    order,
                    PurchaseFailureReason.StoreNotConnected,
                    "Unable to confirm purchase - store is not connected. Please check your internet connection and try again."));
            }

            try
            {
                // TODO: IAP-4074fulfill order
                m_ConfirmOrderUseCase.ConfirmOrder(
                    order,
                    (pendingOrder, resultOrder) =>
                    {
                        switch (resultOrder)
                        {
                            case ConfirmedOrder confirmedOrder:
                                OnConfirmSucceeded(pendingOrder, confirmedOrder);
                                break;
                            case FailedOrder failedOrder:
                                OnConfirmFailed(failedOrder);
                                break;
                            default:
                                OnConfirmFailed(new FailedOrder(
                                    pendingOrder,
                                    PurchaseFailureReason.Unknown,
                                    $"Received invalid order type after confirmation: {resultOrder.GetType()}"));
                                break;
                        }
                    }
                );
            }
            catch (Exception e)
            {
                OnConfirmFailed(new FailedOrder(
                    order,
                    PurchaseFailureReason.Unknown,
                    e.Message));
            }
        }

        void OnConfirmSucceeded(PendingOrder pendingOrder, ConfirmedOrder confirmedOrder)
        {
            m_Purchases.Remove(pendingOrder);
            m_Purchases.Add(confirmedOrder);

            m_AnalyticsClient.OnPurchaseSucceeded(confirmedOrder);
            OnPurchaseConfirmed?.Invoke(confirmedOrder);
        }

        void OnConfirmFailed(FailedOrder failedOrder)
        {
            OnPurchaseConfirmed?.Invoke(failedOrder);
        }

        public void FetchPurchases()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (OnPurchasesFetched == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.FetchPurchases called without a callback defined for IPurchaseService.OnPurchasesFetched.");
            }

            if (OnPurchasesFetchFailed == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.FetchPurchases called without a callback defined for IPurchaseService.OnPurchasesFetchFailed.");
            }
#endif

            if (!IsStoreConnected())
            {
                OnFetchFailure(new PurchasesFetchFailureDescription(PurchasesFetchFailureReason.StoreNotConnected, "Store not connected."));
            }

            try
            {
                m_FetchPurchasesUseCase.FetchPurchases(OnFetchSuccess, OnFetchFailure);
            }
            catch (Exception e)
            {
                OnFetchFailure(new PurchasesFetchFailureDescription(PurchasesFetchFailureReason.Unknown, e.Message));
            }
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

            foreach (var fetchedPurchase in fetchedPurchases.DeferredOrders)
            {
                m_Purchases.Add(fetchedPurchase);
            }

            OnPurchasesFetched?.Invoke(fetchedPurchases);
        }

        void OnFetchFailure(PurchasesFetchFailureDescription fetchFailed)
        {
            OnPurchasesFetchFailed?.Invoke(fetchFailed);
        }

        public void CheckEntitlement(Product product)
        {
            if (OnCheckEntitlement == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.unityLogger.LogIAPWarning("IPurchaseService.CheckEntitlement called without a callback defined for IPurchaseService.OnCheckEntitlement.");
#endif
                return;
            }

            if (!IsStoreConnected())
            {
                OnEntitlementChecked(new Entitlement(product, null, EntitlementStatus.Unknown, "Store is not connected."));
                return;
            }

            try
            {
                m_CheckEntitlementUseCase.IsProductEntitled(product, OnEntitlementChecked);
            }
            catch (Exception e)
            {
                OnEntitlementChecked(new Entitlement(product, null, EntitlementStatus.Unknown, $"Exception during entitlement check: {e.Message}"));
            }
        }

        void OnEntitlementChecked(Entitlement entitlement)
        {
            UpdateEntitlementOrder(entitlement);
            OnCheckEntitlement?.Invoke(entitlement);
        }

        void UpdateEntitlementOrder(Entitlement entitlement)
        {
            if (entitlement.Product != null)
            {
                switch (entitlement.Status)
                {
                    case EntitlementStatus.EntitledUntilConsumed or EntitlementStatus.EntitledButNotFinished:
                    {
                        foreach (var order in GetPurchases())
                        {
                            if (order is PendingOrder pendingOrder &&
                                pendingOrder.CartOrdered.Items().First()?.Product.definition.storeSpecificId == entitlement.Product.definition.storeSpecificId)
                            {
                                entitlement.Order = pendingOrder;
                                break;
                            }
                        }

                        break;
                    }
                    case EntitlementStatus.FullyEntitled:
                    {
                        foreach (var order in GetPurchases())
                        {
                            if (order is ConfirmedOrder confirmedOrder &&
                                confirmedOrder.CartOrdered.Items().First()?.Product.definition.storeSpecificId == entitlement.Product.definition.storeSpecificId)
                            {
                                entitlement.Order = confirmedOrder;
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public void RestoreTransactions(Action<bool, string?>? callback)
        {
            if (!IsStoreConnected())
            {
                callback?.Invoke(false, "Store not connected.");
                return;
            }

            try
            {
                RestoreTransactionsInternal(callback);
            }
            catch (Exception e)
            {
                callback?.Invoke(false, e.Message);
            }
        }

        protected virtual void RestoreTransactionsInternal(Action<bool, string?>? callback)
        {
            callback?.Invoke(false, Application.platform + " is not a supported platform for the restore button");
        }

        /// <summary>
        /// Gets an observable collection of the purchases fetched, confirmed and ordered.
        /// </summary>
        /// <returns>The read-only observable collection of all purchases made.</returns>
        public ReadOnlyObservableCollection<Order> GetPurchases()
        {
            return m_PurchasesReadOnly;
        }

        internal bool IsStoreConnected()
        {
            return m_StoreWrapper.GetStoreConnectionState() == ConnectionState.Connected;
        }
    }
}
