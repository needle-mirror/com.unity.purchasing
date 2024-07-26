using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Public Interface to check if a given product has been entitled.
    /// </summary>
    public interface ICheckEntitlementUseCase
    {
        /// <summary>
        /// Check if the product is entitled or not.
        /// </summary>
        /// <param name="product">The product to check for entitlement.</param>
        /// <returns>True if the product is entitled, false if not.</returns>
        void IsProductEntitled(Product product, Action<Entitlement> onCheckComplete);
    }
}
