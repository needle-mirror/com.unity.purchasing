using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class LegacyAnalyticsAdapter : IAnalyticsAdapter
    {
        readonly ILegacyUnityAnalytics m_LegacyAnalytics;

        public LegacyAnalyticsAdapter(ILegacyUnityAnalytics legacyAnalytics)
        {
            m_LegacyAnalytics = legacyAnalytics;
        }

        public void SendTransactionEvent(Product product)
        {
            m_LegacyAnalytics.SendTransactionEvent(product.definition.storeSpecificId,
                product.metadata.localizedPrice,
                product.metadata.isoCurrencyCode,
                product.receipt,
                null);
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureReason reason)
        {
            var data = new Dictionary<string, object>()
            {
                {"productID", product.definition.storeSpecificId},
                {"reason", reason},
                {"price", product.metadata.localizedPrice},
                {"currency", product.metadata.isoCurrencyCode}
            };
            m_LegacyAnalytics.SendCustomEvent("unity.PurchaseFailed", data);
        }
    }
}
