#if IAP_LEGACY_ANALYTICS_SERVICE_ENABLED

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class LegacyUnityAnalytics : ILegacyUnityAnalytics
    {
        public void SendTransactionEvent(string productId, Decimal amount, string currency, string receiptPurchaseData,
            string signature)
        {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            Analytics.Analytics.Transaction(productId, amount, currency, receiptPurchaseData, signature);
#endif
        }

        public void SendCustomEvent(string name, Dictionary<string, object> data)
        {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            Analytics.Analytics.CustomEvent(name, data);
#endif
        }
    }
}

#endif
