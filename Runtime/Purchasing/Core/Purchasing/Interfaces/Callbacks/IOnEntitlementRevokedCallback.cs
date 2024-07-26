using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    public interface IOnEntitlementRevokedCallback
    {
        /// <summary>
        /// Inform whenever an entitlement is revoked.
        /// </summary>
        /// <param name="productIds">The product ids being revoked.</param>
        void OnEntitlementsRevoked(List<string> productIds);
    }
}
