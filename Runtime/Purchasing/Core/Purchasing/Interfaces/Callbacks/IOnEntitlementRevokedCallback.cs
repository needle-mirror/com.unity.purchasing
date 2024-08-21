using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    public interface IOnEntitlementRevokedCallback
    {
        /// <summary>
        /// Inform whenever an entitlement is revoked.
        /// </summary>
        /// <param name="productId">The product id being revoked.</param>
        void onEntitlementRevoked(string productId);
    }
}
