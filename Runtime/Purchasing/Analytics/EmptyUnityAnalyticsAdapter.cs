using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    class EmptyAnalyticsAdapter : IAnalyticsAdapter
    {
        public void SendTransactionEvent(Product product)
        {
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureReason reason)
        {
        }
    }
}
