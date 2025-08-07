using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for receiving callbacks when an entitlement is revoked.
    /// </summary>
    public interface IOnEntitlementRevokedCallback
    {
        /// <summary>
        /// Inform whenever an entitlement is revoked.
        /// </summary>
        /// <param name="productId">The product id being revoked.</param>
        void onEntitlementRevoked(string productId);
    }
}
