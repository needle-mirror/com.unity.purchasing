#nullable enable
using System;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Public Interface for a service responsible for ordering products, fetching previous purchases and validating product entitlements.
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>
        /// Apple Specific Purchase Extensions
        /// </summary>
        IAppleStoreExtendedPurchaseService? Apple { get; }

        /// <summary>
        /// Google Specific Purchase Extensions
        /// </summary>
        IGooglePlayStoreExtendedPurchaseService? Google { get; }

        /// <summary>
        /// Purchase a product.
        /// Be sure to first register callbacks via `AddPurchasePendingAction` and `AddPurchaseFailedAction`.
        /// </summary>
        /// <param name="product">The Product to purchase.</param>
        void PurchaseProduct(Product product);

        /// <summary>
        /// Purchase a cart.
        /// Be sure to first register your callback via `OnPurchasePending` and `OnPurchaseFailed`.
        /// </summary>
        /// <param name="cart">The cart to purchase.</param>
        void Purchase(ICart cart);

        /// <summary>
        /// Confirm a pending order.
        /// Be sure to first register your callbacks via `OnPurchaseConfirmed`.
        /// </summary>
        /// <param name="order">The pending order to confirm.</param>
        void ConfirmPurchase(PendingOrder order);

        /// <summary>
        /// Fetch pre-existing purchases.
        /// Be sure to first register your callbacks via `OnPurchasesFetched` and `OnPurchasesFetchFailed`.
        /// </summary>
        void FetchPurchases();

        /// <summary>
        /// Check if a Product has been entitled.
        /// Be sure to first register your callback via `OnCheckEntitlement`.
        /// </summary>
        /// <param name="product">The Product to check for entitlement.</param>
        void CheckEntitlement(Product product);

        /// <summary>
        /// Initiate a request to restore previously made purchases.
        /// </summary>
        /// <param name="callback">Action will be called when the request to restore transactions comes back. The bool will be true if it was successful or false if it was not.
        /// The string is an optional error message.</param>
        void RestoreTransactions(Action<bool, string?>? callback);

        /// <summary>
        /// Gets an observable collection of the purchases fetched, confirmed and ordered.
        /// </summary>
        /// <returns>The read-only observable collection of all purchases made.</returns>
        ReadOnlyObservableCollection<Order> GetPurchases();

        /// <summary>
        /// Sets whether to process pending orders when purchases are fetched by sending them to the `OnPurchasePending`
        /// callback.
        /// Default is <c>true</c>.
        /// </summary>
        /// <param name="shouldProcess">Whether to process pending orders when purchases are fetched.</param>
        void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess);

        /// <summary>
        /// Callback when a purchase has been paid, but hasn't been confirmed yet.
        /// </summary>
        event Action<PendingOrder>? OnPurchasePending;

        /// <summary>
        /// Callback when a purchase has been confirmed.
        /// This can pass a `ConfirmedOrder` or a `FailedOrder` depending on the purchase outcome.
        /// </summary>
        event Action<Order>? OnPurchaseConfirmed;

        /// <summary>
        /// Callback when products orders fail.
        /// </summary>
        event Action<FailedOrder>? OnPurchaseFailed;

        /// <summary>
        /// Callback when products orders are deferred.
        /// </summary>
        event Action<DeferredOrder>? OnPurchaseDeferred;

        /// <summary>
        /// Callback when previous purchases are fetched.
        /// </summary>
        event Action<Orders>? OnPurchasesFetched;

        /// <summary>
        /// Callback when an attempt to fetch previous purchases has failed.
        /// </summary>
        event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed;

        /// <summary>
        /// Callback when a check for product entitlement is complete.
        /// </summary>
        event Action<Entitlement>? OnCheckEntitlement;
    }
}
