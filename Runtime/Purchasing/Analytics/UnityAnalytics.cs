using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    ///     Forward transaction information to Unity Analytics.
    /// </summary>
    class UnityAnalytics : IUnityAnalytics
    {
        public void SendTransactionEvent(Product product)
        {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            Analytics.Analytics.Transaction(product.definition.storeSpecificId,
                product.metadata.localizedPrice,
                product.metadata.isoCurrencyCode,
                product.receipt,
                null);
#endif
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(product.receipt);
            var analyticsReceipt = unifiedReceipt.ToReceiptAndSignature();
            var txParams = BuildTransactionParameters(product, unifiedReceipt, analyticsReceipt);

            AnalyticsService.Instance.Transaction(txParams);
        }

        public void SendCustomEvent(string name, Dictionary<string, object> data)
        {
#if ENABLE_CLOUD_SERVICES_ANALYTICS
            Analytics.Analytics.CustomEvent(name, data);
#endif
        }

        static TransactionParameters BuildTransactionParameters(Product product, UnifiedReceipt unifiedReceipt, AnalyticsTransactionReceipt analyticsReceipt)
        {
            var txParams = new TransactionParameters
            {
                ProductID = product.definition.storeSpecificId,
                TransactionName = product.metadata.localizedTitle,
                TransactionID = unifiedReceipt.TransactionID,
                TransactionType = TransactionType.PURCHASE,
                TransactionReceipt = analyticsReceipt.transactionReceipt,
                TransactionReceiptSignature = analyticsReceipt.transactionReceiptSignature,
                TransactionServer = analyticsReceipt.transactionServer,
                ProductsReceived = new Unity.Services.Analytics.Product
                {
                    Items = new List<Item>
                    {
                        new Item
                        {
                            ItemName = product.definition.id,
                            ItemType = product.definition.type.ToString(),
                            ItemAmount = 1
                        }
                    }
                },
                ProductsSpent = new Unity.Services.Analytics.Product
                {
                    RealCurrency = new RealCurrency
                    {
                        RealCurrencyType = product.metadata.isoCurrencyCode,
                        RealCurrencyAmount = AnalyticsService.Instance.ConvertCurrencyToMinorUnits(product.metadata.isoCurrencyCode, (double)product.metadata.localizedPrice)
                    }
                }
            };
            return txParams;
        }
    }
}
