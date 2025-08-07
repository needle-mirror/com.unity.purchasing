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
        /// <param name="product">Product to change visibility.</param>
        /// <param name="visibility">The new product visibility.</param>
        void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visibility);
    }
}
