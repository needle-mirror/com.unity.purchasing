using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    interface ILegacyUnityAnalytics
    {
        void SendTransactionEvent(string productId, Decimal amount, string currency, string receiptPurchaseData,
            string signature);

        void SendCustomEvent(string name, Dictionary<string, object> data);
    }
}
