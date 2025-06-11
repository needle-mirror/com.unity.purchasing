#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
using System;
using Unity.Services.Analytics;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    static class UnifiedReceiptAnalyticsExtensions
    {
        public static AnalyticsTransactionReceipt ToReceiptAndSignature(this UnifiedReceipt receipt)
        {
            var analyticsReceipt = new AnalyticsTransactionReceipt
            {
                transactionServer = receipt.ToTransactionServer()
            };

            if (analyticsReceipt.transactionServer == TransactionServer.GOOGLE)
            {
                var googleReceipt = JsonUtility.FromJson<GoogleReceipt>(receipt.Payload);

                analyticsReceipt.transactionReceipt = googleReceipt?.json;
                analyticsReceipt.transactionReceiptSignature = googleReceipt?.signature;
            }
            else
            {
                analyticsReceipt.transactionReceipt = receipt.Payload;
            }

            return analyticsReceipt;
        }

        static TransactionServer? ToTransactionServer(this UnifiedReceipt receipt)
        {
            if (receipt.Store == null)
            {
                return null;
            }

            var store = receipt.Store.ToLower();

            if (store.Contains("mac") || store.Contains("apple"))
            {
                return TransactionServer.APPLE;
            }

            if (store.Contains("google"))
            {
                return TransactionServer.GOOGLE;
            }

            return null;
        }
    }
}
#endif
