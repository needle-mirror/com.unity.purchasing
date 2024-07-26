using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Public Interface to get notified when an entitlement is revoked
    /// </summary>
    public interface IOnEntitlementRevokedUseCase
    {
        /// <summary>
        /// Action called when entitlement is revoked
        /// </summary>
        event Action<List<string>> RevokedEntitlementAction;
    }
}
