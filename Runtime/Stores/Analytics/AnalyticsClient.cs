using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class AnalyticsClient : IAnalyticsClient
    {
        readonly IAnalyticsAdapter m_Analytics;

        [Preserve]
        internal AnalyticsClient(IAnalyticsAdapter analytics)
        {
            m_Analytics = analytics;
        }

        public void OnPurchaseSucceeded(ConfirmedOrder confirmedOrder)
        {
            foreach (var cartItem in confirmedOrder.CartOrdered.Items())
            {
                m_Analytics.SendTransactionEvent(cartItem, confirmedOrder.Info.Receipt);
            }
        }

        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                var description = new PurchaseFailureDescription(cartItem.Product, failedOrder.FailureReason, failedOrder.Details);
                m_Analytics.SendTransactionFailedEvent(description);
            }
        }
    }
}
