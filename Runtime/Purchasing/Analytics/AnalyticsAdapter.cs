using System.Collections.Generic;
using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    class AnalyticsAdapter : IAnalyticsAdapter
    {
        IAnalyticsService m_Analytics;

        public AnalyticsAdapter(IAnalyticsService analytics)
        {
            m_Analytics = analytics;
        }

        public void SendTransactionEvent(Product product)
        {
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(product.receipt);
            var analyticsReceipt = unifiedReceipt.ToReceiptAndSignature();
            var txParams = BuildTransactionParameters(product, unifiedReceipt, analyticsReceipt);
            m_Analytics.Transaction(txParams);
        }

        TransactionParameters BuildTransactionParameters(Product product,
            UnifiedReceipt unifiedReceipt, AnalyticsTransactionReceipt analyticsReceipt)
        {
            return new TransactionParameters
            {
                ProductID = product.definition.storeSpecificId,
                TransactionName = product.metadata.localizedTitle,
                TransactionID = unifiedReceipt.TransactionID,
                TransactionType = TransactionType.PURCHASE,
                TransactionReceipt = analyticsReceipt.transactionReceipt,
                TransactionReceiptSignature = analyticsReceipt.transactionReceiptSignature,
                TransactionServer = analyticsReceipt.transactionServer,
                ProductsReceived = GenerateItemReceivedForPurchase(product),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(product)
            };
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureReason reason)
        {
            var transactionFailedParameters = BuildTransactionFailedParameters(product, reason);
            m_Analytics.TransactionFailed(transactionFailedParameters);
        }

        TransactionFailedParameters BuildTransactionFailedParameters(Product product,
            PurchaseFailureReason reason)
        {
            return new TransactionFailedParameters
            {
                ProductID = product.definition.storeSpecificId,
                TransactionName = product.metadata.localizedTitle,
                TransactionType = TransactionType.PURCHASE,
                ProductsReceived = GenerateItemReceivedForPurchase(product),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(product),
                FailureReason = reason.ToString()
            };
        }

        Unity.Services.Analytics.Product GenerateItemReceivedForPurchase(Product product)
        {
            return new Unity.Services.Analytics.Product
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
            };
        }

        Unity.Services.Analytics.Product GenerateRealCurrencySpentOnPurchase(Product product)
        {
            return new Unity.Services.Analytics.Product
            {
                RealCurrency = new RealCurrency
                {
                    RealCurrencyType = product.metadata.isoCurrencyCode,
                    RealCurrencyAmount = ExtractRealCurrencyAmount(product)
                }
            };
        }

        long ExtractRealCurrencyAmount(Product product)
        {
            return m_Analytics.ConvertCurrencyToMinorUnits(product.metadata.isoCurrencyCode,
                (double)product.metadata.localizedPrice);
        }
    }
}
