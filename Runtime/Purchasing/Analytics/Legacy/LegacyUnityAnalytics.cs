using System;
using System.Collections.Generic;
using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    class LegacyUnityAnalytics : ILegacyUnityAnalytics
    {
        public void SendTransactionEvent(string productId, Decimal amount, string currency, string receiptPurchaseData,
            string signature)
        {
            Analytics.Analytics.Transaction(productId, amount, currency, receiptPurchaseData, signature);
        }

        public void SendCustomEvent(string name, Dictionary<string, object> data)
        {
            Analytics.Analytics.CustomEvent(name, data);
        }
    }
}
