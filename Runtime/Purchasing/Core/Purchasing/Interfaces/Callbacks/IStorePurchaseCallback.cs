namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for a class that handles callbacks for purchasing products from a Store.
    /// </summary>
    public interface IStorePurchaseCallback
    {
        /// <summary>
        /// Inform Unity Purchasing of a purchase.
        /// </summary>
        /// <param name="order"> The pending order that was made. </param>
        void OnPurchaseSucceeded(PendingOrder order);

        /// <summary>
        /// Notify a failed purchase with associated details.
        /// </summary>
        /// <param name="failedOrder"> The object detailing the purchase failure </param>
        void OnPurchaseFailed(FailedOrder failedOrder);

        /// <summary>
        /// Notify a deferred purchase with associated details.
        /// </summary>
        /// <param name="deferredOrder"> The deferred order that was made. </param>
        void OnPurchaseDeferred(DeferredOrder deferredOrder);
    }
}
