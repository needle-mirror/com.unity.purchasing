using System;

namespace UnityEngine.Purchasing
{
    internal class CheckEntitlementRequest
    {
        internal Product ProductToCheck { get; }
        internal Action<Entitlement> OnChecked { get; }

        internal CheckEntitlementRequest(Product product, Action<Entitlement> onChecked)
        {
            ProductToCheck = product;
            OnChecked = onChecked;
        }
    }
}
