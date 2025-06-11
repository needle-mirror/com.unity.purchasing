using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreChangeSubscriptionService : IGooglePlayStoreChangeSubscriptionService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;

        [Preserve]
        internal GooglePlayStoreChangeSubscriptionService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void ChangeSubscription(ProductDefinition product, Order currentOrder,
            GooglePlayReplacementMode? desiredReplacementMode)
        {
            m_GooglePlayStoreService.Purchase(product, currentOrder, desiredReplacementMode);
        }
    }
}
