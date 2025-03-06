#if DISABLE_RUNTIME_IAP_ANALYTICS || (!IAP_ANALYTICS_SERVICE_ENABLED && !IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT)
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class EmptyAnalyticsAdapter : IAnalyticsAdapter
    {
        public void SendTransactionEvent(CartItem item, string receipt) { }

        public void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription) { }
    }
}
#endif
