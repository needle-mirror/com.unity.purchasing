using System;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreCheckEntitlementService : IGooglePlayStoreCheckEntitlementService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreCheckEntitlementCallback m_EntitlementCallback;

        [Preserve]
        internal GooglePlayStoreCheckEntitlementService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void CheckEntitlement(ProductDefinition product)
        {
            m_GooglePlayStoreService.CheckEntitlement(product, (checkedProduct, status) =>
            {
                m_EntitlementCallback?.OnCheckEntitlement(checkedProduct, status);
            });
        }

        public void SetCheckEntitlementCallback(IStoreCheckEntitlementCallback entitlementCallback)
        {
            m_EntitlementCallback = entitlementCallback;
        }
    }
}
