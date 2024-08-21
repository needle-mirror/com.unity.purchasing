using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class OnEntitlementRevokedUseCase : IOnEntitlementRevokedUseCase, IOnEntitlementRevokedCallback
    {
        public event Action<string> RevokedEntitlementAction;

        /// <summary>
        /// Create the use case object for a store.
        /// </summary>
        /// <param name="storeResponsible">The store responsible for the entitlement to be revoked</param>
        [Preserve]
        internal OnEntitlementRevokedUseCase(IStore storeResponsible)
        {
            storeResponsible.SetOnRevokedEntitlementCallback(this);
        }

        public void onEntitlementRevoked(string productId)
        {
            RevokedEntitlementAction?.Invoke(productId);
        }
    }
}
