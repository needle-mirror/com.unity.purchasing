#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of setting store promotion order.
    /// </summary>
    interface ISetStorePromotionOrderUseCase
    {
        /// <summary>
        /// Set store promotion order.
        /// </summary>
        /// <param name="storeSpecificIds">The Apple store-specific ids of the catalog listings to promote, in order.</param>
        void SetStorePromotionOrder(List<string> storeSpecificIds);
    }
}
