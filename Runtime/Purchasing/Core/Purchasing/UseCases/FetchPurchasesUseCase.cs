using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Use case for fetching pre-existing product purchases.
    /// </summary>
    class FetchPurchasesUseCase : IFetchPurchasesUseCase, IStorePurchaseFetchCallback
    {
        readonly IStore m_Store;

        event Action<Orders> FetchSuccessAction;
        event Action<PurchasesFetchFailureDescription> FetchFailureAction;

        /// <summary>
        /// Create the use case object for a store.
        /// </summary>
        /// <param name="storeResponsible">The store responsible for the purchases to be retrieved</param>
        [Preserve]
        internal FetchPurchasesUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetPurchaseFetchCallback(this);
        }

        /// <summary>
        /// Fetch all purchases that have been made, usually asynchronously. Success or failure is signalled via the actions passed.
        /// </summary>
        /// <param name="fetchSuccessAction">The event called when the fetch is successful.</param>
        /// <param name="fetchFailureAction">The event called when the fetch fails.</param>
        public void FetchPurchases(Action<Orders> fetchSuccessAction, Action<PurchasesFetchFailureDescription> fetchFailureAction)
        {
            FetchSuccessAction = fetchSuccessAction;
            FetchFailureAction = fetchFailureAction;

            m_Store.FetchPurchases();
        }

        public void OnAllPurchasesRetrieved(IReadOnlyList<Order> orders)
        {
            var confirmedOrders = orders.OfType<ConfirmedOrder>().ToList().AsReadOnly();
            var pendingOrders = orders.OfType<PendingOrder>().ToList().AsReadOnly();
            var deferredOrders = orders.OfType<DeferredOrder>().ToList().AsReadOnly();
            var filteredOrders = new Orders(confirmedOrders, pendingOrders, deferredOrders);

            FetchSuccessAction?.Invoke(filteredOrders);
        }

        /// <summary>
        /// Inform Unity Purchasing of a failure to retrieve purchases.
        /// </summary>
        /// <param name="failureReason"> The reason that purchases could not be retrieved. </param>
        public void OnPurchasesRetrievalFailed(PurchasesFetchFailureDescription failureReason)
        {
            FetchFailureAction?.Invoke(failureReason);
        }
    }
}
