using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface to get notified when an entitlement is revoked
    /// </summary>
    interface IOnEntitlementRevokedUseCase
    {
        /// <summary>
        /// Action called when entitlement is revoked
        /// </summary>
        event Action<string> OnEntitlementRevoked;
    }
}
