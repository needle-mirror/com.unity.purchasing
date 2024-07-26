#nullable enable
using System;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Public Interface for a service responsible for ordering products, fetching previous purchases and validating product entitlements.
    /// Tasks requiring a connection to Stores (Apple App Store or Google Play Store for example)
    /// executed without ConnectionState.Connected will throw a StoreConnectionException.
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>
        /// Apple Specific Purchase Extensions
        /// </summary>
        public IAppleStoreExtendedPurchaseService? Apple { get; }

        /// <summary>
        /// Google Specific Purchase Extensions
        /// </summary>
        public IGooglePlayStoreExtendedPurchaseService? Google { get; }

        /// <summary>
        /// Purchase a product.
        /// </summary>
        /// <param name="product">The Product to purchase.</param>
        void PurchaseProduct(Product product);

        /// <summary>
        /// Purchase a cart.
        /// </summary>
        /// <param name="cart">The cart to purchase.</param>
        void Purchase(ICart cart);

        /// <summary>
        /// Confirm a pending order.
        /// </summary>
        /// <param name="order">The pending order to confirm.</param>
        void ConfirmOrder(PendingOrder order);

        /// <summary>
        /// Fetch pre-existing purchases.
        /// </summary>
        void FetchPurchases();

        /// <summary>
        /// Check if a Product has been entitled.
        /// </summary>
        /// <param name="product">The Product to check for entitlements.</param>
        void IsProductEntitled(Product product);

        /// <summary>
        /// Initiate a request to restore previously made purchases.
        /// </summary>
        /// <param name="callback">Action will be called when the request to restore transactions comes back. The bool will be true if it was successful or false if it was not.
        /// The string is an optional error message.</param>
        void RestoreTransactions(Action<bool, string?> callback);

        /// <summary>
        /// Gets an observable collection of the purchases fetched, confirmed and ordered.
        /// </summary>
        /// <returns>The read-only observable collection of all purchases made.</returns>
        ReadOnlyObservableCollection<Order> GetPurchases();

        /// <summary>
        /// Add an action to be called when products are ordered.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        void AddPendingOrderUpdatedAction(Action<PendingOrder>? updatedAction);

        /// <summary>
        /// Add an action to be called when product orders are confirmed.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        void AddConfirmedOrderUpdatedAction(Action<ConfirmedOrder>? updatedAction);

        /// <summary>
        /// Add an action to be called when products orders fail.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        void AddPurchaseFailedAction(Action<FailedOrder>? failedAction);

        /// <summary>
        /// Add an action to be called when products orders are deferred.
        /// </summary>
        /// <param name="deferredAction">The action to be added to the list of callbacks.</param>
        void AddPurchaseDeferredAction(Action<DeferredOrder>? deferredAction);

        /// <summary>
        /// Add an action to be called when previous purchases are fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        void AddFetchedPurchasesAction(Action<Orders>? updatedAction);

        /// <summary>
        /// Add an action to be called when an attempt to fetch previous purchases has failed.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        void AddFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription>? failedAction);

        /// <summary>
        /// Add an action to be called when a check for product entitlement is complete.
        /// </summary>
        /// <param name="checkEntitlementAction">The action to be added to the list of callbacks.</param>
        void AddCheckEntitlementAction(Action<Entitlement>? checkEntitlementAction);

        /// <summary>
        /// Remove an action to be called when products are ordered.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        void RemovePendingOrderUpdatedAction(Action<PendingOrder>? updatedAction);

        /// <summary>
        /// Remove an action to be called when product orders are confirmed.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        void RemoveConfirmedOrderUpdatedAction(Action<ConfirmedOrder>? updatedAction);

        /// <summary>
        /// Remove an action to be called when products orders fail.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        void RemovePurchaseFailedAction(Action<FailedOrder>? failedAction);

        /// <summary>
        /// Remove an action to be called when previous purchases are fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        void RemoveFetchedPurchasesAction(Action<Orders>? updatedAction);

        /// <summary>
        /// Remove an action to be called when an attempt to fetch previous purchases has failed.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        void RemoveFetchPurchasesFailedAction(Action<PurchasesFetchFailureDescription>? failedAction);

        /// <summary>
        /// Remove an action to be called when a check for product entitlement is complete.
        /// </summary>
        /// <param name="checkEntitlementAction">The action to be added to the list of callbacks.</param>
        void RemoveCheckEntitlementAction(Action<Entitlement>? checkEntitlementAction);
    }
}
