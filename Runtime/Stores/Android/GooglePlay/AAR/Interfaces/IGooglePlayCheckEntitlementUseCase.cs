using System;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePlayCheckEntitlementUseCase
    {
        void CheckEntitlement(ProductDefinition product, Action<ProductDefinition, EntitlementStatus> onEntitlementChecked);
    }
}
