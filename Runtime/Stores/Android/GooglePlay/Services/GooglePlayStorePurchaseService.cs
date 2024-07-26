using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStorePurchaseService : IGooglePlayStorePurchaseService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;

        [Preserve]
        internal GooglePlayStorePurchaseService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void Purchase(ProductDefinition product)
        {
            m_GooglePlayStoreService.Purchase(product);
        }
    }
}
