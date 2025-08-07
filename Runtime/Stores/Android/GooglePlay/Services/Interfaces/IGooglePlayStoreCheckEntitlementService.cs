using System;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePlayStoreCheckEntitlementService
    {
        void CheckEntitlement(ProductDefinition product);
        void SetCheckEntitlementCallback(IStoreCheckEntitlementCallback entitlementCallback);
    }
}
