#if IAP_ANALYTICS_SERVICE_ENABLED

using System;
using System.Collections.Generic;
using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    class AnalyticsAdapter : IAnalyticsAdapter
    {
        readonly IAnalyticsServiceWrapper m_Analytics;
        readonly ILogger m_Logger;
        TransactionEventHelper m_TransactionEventHelper;

        internal AnalyticsAdapter(IAnalyticsServiceWrapper analytics, ILogger logger)
        {
            m_Analytics = analytics;
            m_Logger = logger;
            m_TransactionEventHelper = new TransactionEventHelper(analytics, logger);
        }

        public void SendTransactionEvent(CartItem item, string receipt)
        {
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(receipt);
            var analyticsReceipt = unifiedReceipt.ToReceiptAndSignature();
            var txParams = BuildTransactionParameters(item, unifiedReceipt, analyticsReceipt);
            m_Analytics.AnalyticsServiceInstance()?.Transaction(txParams);
        }

        TransactionParameters BuildTransactionParameters(CartItem item,
            UnifiedReceipt unifiedReceipt, AnalyticsTransactionReceipt analyticsReceipt)
        {
            return new TransactionParameters
            {
                ProductID = item.Product.definition.storeSpecificId,
                TransactionName = CommonTransactionEventHelper.GetTransactionName(item.Product),
                TransactionID = unifiedReceipt.TransactionID,
                TransactionType = TransactionType.PURCHASE,
                TransactionReceipt = analyticsReceipt.transactionReceipt,
                TransactionReceiptSignature = analyticsReceipt.transactionReceiptSignature,
                TransactionServer = analyticsReceipt.transactionServer,
                ProductsReceived = GenerateItemReceivedForPurchase(item),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(item.Product)
            };
        }

        public void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription)
        {
            var transactionFailedParameters = BuildTransactionFailedParameters(failureDescription.item, TransactionFailedEventHelper.BuildFailureReason(failureDescription));
            m_Analytics.AnalyticsServiceInstance()?.TransactionFailed(transactionFailedParameters);
        }

        TransactionFailedParameters BuildTransactionFailedParameters(CartItem item,
            string failureReason)
        {
            return new TransactionFailedParameters
            {
                ProductID = item.Product.definition.storeSpecificId,
                TransactionName = CommonTransactionEventHelper.GetTransactionName(item.Product),
                TransactionType = TransactionType.PURCHASE,
                ProductsReceived = GenerateItemReceivedForPurchase(item.Product),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(item.Product),
                FailureReason = failureReason
            };
        }

        Unity.Services.Analytics.Product GenerateItemReceivedForPurchase(CartItem item)
        {
            return new Unity.Services.Analytics.Product
            {
                Items = new List<Item>
                {
                    new Item
                    {
                        ItemName = item.Product.definition.id,
                        ItemType = item.Product.definition.type.ToString(),
                        ItemAmount = item.Quantity
                    }
                }
            };
        }

        Unity.Services.Analytics.Product GenerateRealCurrencySpentOnPurchase(Product product)
        {
            return new Unity.Services.Analytics.Product
            {
                RealCurrency = CreateRealCurrencyFromProduct(product)
            };
        }

        RealCurrency CreateRealCurrencyFromProduct(Product product)
        {
            return new RealCurrency
            {
                RealCurrencyType = product.metadata.isoCurrencyCode ?? "",
                RealCurrencyAmount = m_TransactionEventHelper.CheckCurrencyCodeAndExtractRealCurrencyAmount(product)
            };
        }
    }
}

#endif
