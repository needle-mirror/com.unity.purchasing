#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of fetching store promotion visibility.
    /// </summary>
    interface IFetchStorePromotionVisibilityUseCase
    {
        /// <summary>
        /// Fetch store promotion visibility
        /// </summary>
        /// <param name="product">The event called when products are successfully fetched.</param>
        /// <param name="successCallback">The event called when products are successfully fetched.</param>
        /// <param name="errorCallback">The event called when products could not be fetched.</param>
        void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback);
    }
}
