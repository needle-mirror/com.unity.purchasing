using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class EmptyAnalyticsAdapter : IAnalyticsAdapter
    {
        public void SendTransactionEvent(Product product) { }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureDescription reason) { }
    }
}
