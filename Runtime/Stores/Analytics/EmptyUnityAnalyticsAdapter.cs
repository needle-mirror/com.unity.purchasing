using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class EmptyAnalyticsAdapter : IAnalyticsAdapter
    {
        public void SendTransactionEvent(CartItem item, string receipt) { }

        public void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription) { }
    }
}
