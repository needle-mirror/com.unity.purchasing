using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class AnalyticsClient : IAnalyticsClient
    {
        readonly IAnalyticsAdapter m_Analytics;
        readonly IAnalyticsAdapter m_LegacyAnalytics;

        public AnalyticsClient(IAnalyticsAdapter analytics, IAnalyticsAdapter legacyAnalytics)
        {
            m_Analytics = analytics;
            m_LegacyAnalytics = legacyAnalytics;
        }

        public void OnPurchaseSucceeded(Product product)
        {
            if (product.metadata.isoCurrencyCode == null)
            {
                return;
            }

            m_Analytics.SendTransactionEvent(product);
            m_LegacyAnalytics.SendTransactionEvent(product);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            m_Analytics.SendTransactionFailedEvent(product, reason);
            m_LegacyAnalytics.SendTransactionFailedEvent(product, reason);
        }
    }
}
