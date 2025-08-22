
#nullable enable

using System;
using System.Collections.Generic;
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
        bool m_ProcessFetchedPendingOrders = true;
        readonly HashSet<string> m_PurchasesProcessedInSession = new();

#if IAP_TX_VERIFIER_ENABLED
            readonly Store m_StoreName;
            readonly ITransactionVerifier m_TransactionVerifier;
#endif

        /// <summary>
        /// Configures whether pending orders should be automatically processed when purchases are fetched from the store.
        /// </summary>
        /// <param name="shouldProcess">True to automatically process pending orders after fetching purchases, false to skip processing.</param>
        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            m_ProcessFetchedPendingOrders = shouldProcess;
        }

        /// <summary>
        /// Event triggered when a purchase is pending confirmation or processing.
        /// Subscribe to this event to handle purchases that require additional processing before completion.
        /// </summary>
        public event Action<PendingOrder>? OnPurchasePending;

        /// <summary>
        /// Event triggered when a purchase has been successfully confirmed and completed.
        /// Subscribe to this event to handle successful purchase completion, such as granting in-game content.
        /// </summary>
        public event Action<Order>? OnPurchaseConfirmed;

        /// <summary>
        /// Event triggered when a purchase has failed to complete.
        /// Subscribe to this event to handle purchase failures and provide appropriate user feedback.
        /// </summary>
        public event Action<FailedOrder>? OnPurchaseFailed;

        /// <summary>
        /// Event triggered when a purchase has been deferred by the store.
        /// This typically occurs when parental approval is required for the purchase.
        /// </summary>
        public event Action<DeferredOrder>? OnPurchaseDeferred;

        /// <summary>
        /// Event triggered when previously made purchases have been successfully fetched from the store.
        /// Subscribe to this event to process and restore previously purchased content.
        /// </summary>
        public event Action<Orders>? OnPurchasesFetched;

        /// <summary>
        /// Event triggered when fetching previous purchases from the store has failed.
        /// Subscribe to this event to handle errors in retrieving purchase history.
        /// </summary>
        public event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed;

        /// <summary>
        /// Event triggered when checking product entitlement status.
        /// Subscribe to this event to handle entitlement verification results.
        /// </summary>
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

        /// <summary>
        /// Gets the Apple-specific purchase service extensions and functionality.
        /// Provides access to Apple App Store specific features and operations.
        /// </summary>
        public IAppleStoreExtendedPurchaseService? Apple => this as IAppleStoreExtendedPurchaseService;

        /// <summary>
        /// Gets the Google-specific purchase service extensions and functionality.
        /// Provides access to Google Play Store specific features and operations.
        /// </summary>
        public IGooglePlayStoreExtendedPurchaseService? Google => this as IGooglePlayStoreExtendedPurchaseService;

        /// <summary>
        /// Initiates a purchase for the specified product.
        /// </summary>
        /// <param name="product">The product to purchase. Must be a valid, purchasable product from the store.</param>
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

        /// <summary>
        /// Initiates a purchase for all products in the specified cart.
        /// </summary>
        /// <param name="cart">The cart containing products to purchase. All products in the cart will be processed for purchase.</param>
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
                    Store.Apple => order.Info.Apple?.jwsRepresentation,
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

        void PurchaseSucceeded(PendingOrder order)
        {
            try
            {
                RemoveDeferredOrders(order);
                m_Purchases.Add(order);

                ProcessPendingOrder(order);
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

        /// <summary>
        /// Confirms a pending purchase order, completing the transaction.
        /// </summary>
        /// <param name="order">The pending order to confirm. This should be a valid pending order received from a purchase event.</param>
        public void ConfirmPurchase(PendingOrder order)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (OnPurchaseConfirmed == null)
            {
                Debug.unityLogger.LogIAPWarning("IPurchaseService.ConfirmPurchase called without a callback defined for IPurchaseService.OnPurchaseConfirmed.");
            }
#endif
            var validationError = ConfirmPurchaseValidations(order);
            if (validationError != null)
            {
                OnConfirmFailed(validationError);
                return;
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

        FailedOrder? ConfirmPurchaseValidations(PendingOrder order)
        {
            if (order == null)
            {
                return new FailedOrder(new Cart(Product.CreateUnknownProduct("InvalidProduct")), PurchaseFailureReason.ProductUnavailable,
                    "Attempting to confirm a null order.");
            }

            if (order.Info == null)
            {
                return new FailedOrder(order, PurchaseFailureReason.Unknown, "Order info is null");
            }

            if (string.IsNullOrEmpty(order.Info.TransactionID))
            {
                return new FailedOrder(order, PurchaseFailureReason.Unknown, "Transaction ID is null or empty");
            }

            if (!IsStoreConnected())
            {
                return new FailedOrder(
                    order,
                    PurchaseFailureReason.StoreNotConnected,
                    "Unable to confirm purchase - store is not connected. Please check your internet connection and try again.");
            }

            // Check if the pending order still exists in our purchases collection as ConfirmedOrder
            var existingConfirmedOrder = m_Purchases.OfType<ConfirmedOrder>()
                .FirstOrDefault(p => p.Info.TransactionID == order.Info.TransactionID);

            if (existingConfirmedOrder != null)
            {
                return new FailedOrder(
                    order,
                    PurchaseFailureReason.ExistingPurchasePending,
                    "Order has already been processed or was not found in pending purchases");
            }
            return null; // No validation errors
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

        /// <summary>
        /// Fetches all previous purchases made by the user from the store.
        /// This will trigger OnPurchasesFetched or OnPurchasesFetchFailed events based on the result.
        /// </summary>
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
            m_Purchases.Clear();
            foreach (var fetchedPurchase in fetchedPurchases.ConfirmedOrders)
            {
                m_Purchases.Add(fetchedPurchase);
            }

            foreach (var fetchedPurchase in fetchedPurchases.PendingOrders)
            {
                m_Purchases.Add(fetchedPurchase);

                if (m_ProcessFetchedPendingOrders && !WasPurchaseAlreadyProcessed(fetchedPurchase.Info.TransactionID))
                {
                    ProcessPendingOrder(fetchedPurchase);
                }
            }

            foreach (var fetchedPurchase in fetchedPurchases.DeferredOrders)
            {
                m_Purchases.Add(fetchedPurchase);
            }

            OnPurchasesFetched?.Invoke(fetchedPurchases);
        }

        bool WasPurchaseAlreadyProcessed(string transactionId)
        {
            return m_PurchasesProcessedInSession.Contains(transactionId);
        }

#if IAP_TX_VERIFIER_ENABLED
        async void ProcessPendingOrder(PendingOrder fetchedPurchase)
        {
            await HandleVerification(fetchedPurchase);
            m_PurchasesProcessedInSession.Add(fetchedPurchase.Info.TransactionID);
        }
#else
        void ProcessPendingOrder(PendingOrder fetchedPurchase)
        {
            OnPurchasePending?.Invoke(fetchedPurchase);
            m_PurchasesProcessedInSession.Add(fetchedPurchase.Info.TransactionID);
        }
#endif

        void OnFetchFailure(PurchasesFetchFailureDescription fetchFailed)
        {
            OnPurchasesFetchFailed?.Invoke(fetchFailed);
        }

        /// <summary>
        /// Checks the entitlement status for the specified product.
        /// This verifies whether the user is entitled to access the product's content.
        /// </summary>
        /// <param name="product">The product to check entitlement for. Must be a valid product from the store.</param>
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

        /// <summary>
        /// Restores previously purchased transactions from the store.
        /// This is typically used to restore purchases on a new device or after reinstalling the app.
        /// </summary>
        /// <param name="callback">Optional callback invoked when the restore operation completes.
        /// The first parameter indicates success (true) or failure (false),
        /// and the second parameter provides an error message if the operation failed.</param>
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

        /// <summary>
        /// Internal implementation for restoring transactions.
        /// Handles the core logic for transaction restoration without external validation.
        /// </summary>
        /// <param name="callback">Optional callback invoked when the restore operation completes.
        /// The first parameter indicates success (true) or failure (false),
        /// and the second parameter provides an error message if the operation failed.</param>
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
