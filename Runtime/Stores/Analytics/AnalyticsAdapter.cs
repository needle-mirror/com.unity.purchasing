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
            item.Product.catalogListings.TryGetValue(item.CatalogListingId, out var listing);
            return new TransactionParameters
            {
                ProductID = listing?.definition?.storeSpecificId,
                TransactionName = CommonTransactionEventHelper.GetTransactionName(listing),
                TransactionID = unifiedReceipt.TransactionID,
                TransactionType = TransactionType.PURCHASE,
                TransactionReceipt = analyticsReceipt.transactionReceipt,
                TransactionReceiptSignature = analyticsReceipt.transactionReceiptSignature,
                TransactionServer = analyticsReceipt.transactionServer,
                ProductsReceived = GenerateItemReceivedForPurchase(item),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(listing)
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
            item.Product.catalogListings.TryGetValue(item.CatalogListingId, out var listing);
            return new TransactionFailedParameters
            {
                ProductID = listing?.definition?.storeSpecificId,
                TransactionName = CommonTransactionEventHelper.GetTransactionName(listing),
                TransactionType = TransactionType.PURCHASE,
                ProductsReceived = GenerateItemReceivedForPurchase(item),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(listing),
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
                        ItemName = item.Product.uSku,
                        ItemType = item.Product.type.ToString(),
                        ItemAmount = item.Quantity
                    }
                }
            };
        }

        Unity.Services.Analytics.Product GenerateRealCurrencySpentOnPurchase(CatalogListing listing)
        {
            return new Unity.Services.Analytics.Product
            {
                RealCurrency = CreateRealCurrencyFromListing(listing)
            };
        }

        RealCurrency CreateRealCurrencyFromListing(CatalogListing listing)
        {
            return new RealCurrency
            {
                RealCurrencyType = listing?.metadata?.isoCurrencyCode ?? "",
                RealCurrencyAmount = m_TransactionEventHelper.CheckCurrencyCodeAndExtractRealCurrencyAmount(listing)
            };
        }
    }
}

#endif
