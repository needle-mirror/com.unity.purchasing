using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for the fetching of pre-existing product purchases.
    /// </summary>
    interface IFetchPurchasesUseCase
    {
        /// <summary>
        /// Fetch all purchases that have been made, usually asynchronously. Success or failure is signalled via the actions passed.
        /// </summary>
        /// <param name="fetchSuccessAction">The event called when the fetch is successful.</param>
        /// <param name="fetchFailureAction">The event called when the fetch fails.</param>
        void FetchPurchases(Action<Orders> fetchSuccessAction, Action<PurchasesFetchFailureDescription> fetchFailureAction);
    }
}
