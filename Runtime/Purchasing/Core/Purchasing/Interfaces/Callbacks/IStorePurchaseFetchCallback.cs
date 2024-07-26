#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for a class that handles callbacks for fetching existing purchases from a Store.
    /// </summary>
    public interface IStorePurchaseFetchCallback
    {
        /// <summary>
        /// Inform Unity Purchasing of all active or pending purchases.
        /// </summary>
        /// <param name="orders"> All active or pending purchased products.</param>
        void OnAllPurchasesRetrieved(IReadOnlyList<Order> orders);

        /// <summary>
        /// Inform Unity Purchasing of a failure to retrieve purchases.
        /// </summary>
        /// <param name="failureReason"> The reason that purchases could not be retrieved. </param>
        void OnPurchasesRetrievalFailed(PurchasesFetchFailureDescription failureReason);
    }
}
