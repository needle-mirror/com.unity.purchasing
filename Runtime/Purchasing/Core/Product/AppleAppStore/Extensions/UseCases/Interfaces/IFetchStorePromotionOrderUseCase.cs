using System;
#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of fetching store promotion order.
    /// </summary>
    interface IFetchStorePromotionOrderUseCase
    {
        /// <summary>
        /// Fetch store promotion order
        /// </summary>
        /// <param name="successCallback">The event called when products are successfully fetched.</param>
        /// <param name="errorCallback">The event called when products could not be fetched.</param>
        void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback);
    }
}
