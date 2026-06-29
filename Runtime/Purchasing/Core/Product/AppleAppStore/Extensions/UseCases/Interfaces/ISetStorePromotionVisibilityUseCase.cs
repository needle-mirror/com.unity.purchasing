#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of setting store promotion order.
    /// </summary>
    interface ISetStorePromotionVisibilityUseCase
    {
        /// <summary>
        /// Override the visibility of a product on the device.
        /// </summary>
        /// <param name="storeSpecificId">The Apple store-specific id of the product whose visibility should be set.</param>
        /// <param name="visibility">The new product visibility.</param>
        void SetStorePromotionVisibility(string storeSpecificId, AppleStorePromotionVisibility visibility);
    }
}
