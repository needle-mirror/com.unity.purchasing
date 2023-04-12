#if IAP_ANALYTICS_SERVICE_ENABLED
using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class AnalyticsAdapter : IAnalyticsAdapter
    {
        readonly IAnalyticsService m_Analytics;
        readonly ILogger m_Logger;

        public AnalyticsAdapter(IAnalyticsService analytics, ILogger logger)
        {
            m_Analytics = analytics;
            m_Logger = logger;
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
                TransactionName = GetTransactionName(product),
                TransactionID = unifiedReceipt.TransactionID,
                TransactionType = TransactionType.PURCHASE,
                TransactionReceipt = analyticsReceipt.transactionReceipt,
                TransactionReceiptSignature = analyticsReceipt.transactionReceiptSignature,
                TransactionServer = analyticsReceipt.transactionServer,
                ProductsReceived = GenerateItemReceivedForPurchase(product),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(product)
            };
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureDescription description)
        {
            var transactionFailedParameters = BuildTransactionFailedParameters(product, BuildFailureReason(description));
            m_Analytics.TransactionFailed(transactionFailedParameters);
        }

        static string BuildFailureReason(PurchaseFailureDescription description)
        {
            var failureReason = $"Failure reason: {description.reason.ToString()}";
            if (string.IsNullOrEmpty(description.message))
            {
                failureReason += $" Failure message: {description.message}";
            }

            return failureReason;
        }

        TransactionFailedParameters BuildTransactionFailedParameters(Product product,
            string failureReason)
        {
            return new TransactionFailedParameters
            {
                ProductID = product.definition.storeSpecificId,
                TransactionName = GetTransactionName(product),
                TransactionType = TransactionType.PURCHASE,
                ProductsReceived = GenerateItemReceivedForPurchase(product),
                ProductsSpent = GenerateRealCurrencySpentOnPurchase(product),
                FailureReason = failureReason
            };
        }

        static string GetTransactionName(Product product)
        {
            return string.IsNullOrEmpty(product.metadata.localizedTitle) ?
                product.definition.storeSpecificId :
                product.metadata.localizedTitle;
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
                RealCurrency = CreateRealCurrencyFromProduct(product)
            };
        }

        RealCurrency CreateRealCurrencyFromProduct(Product product)
        {
            return new RealCurrency
            {
                RealCurrencyType = product.metadata.isoCurrencyCode,
                RealCurrencyAmount = CheckCurrencyCodeAndExtractRealCurrencyAmount(product)
            };
        }

        long CheckCurrencyCodeAndExtractRealCurrencyAmount(Product product)
        {
            if (product.metadata.isoCurrencyCode != null)
            {
                return ExtractRealCurrencyAmount(product);
            }
            else
            {
                m_Logger.LogIAPWarning($"The isoCurrencyCode for product ID {product.definition.id} is null. Were you trying to purchase an unavailable product? The price will be recorded as 0.");
                return 0;
            }
        }

        long ExtractRealCurrencyAmount(Product product)
        {
            try
            {
                return m_Analytics.ConvertCurrencyToMinorUnits(product.metadata.isoCurrencyCode, (double)product.metadata.localizedPrice);
            }
            catch (Exception)
            {
                m_Logger.LogIAPWarning($"Could not convert real currency amount payable for product ID {product.definition.id}. The price will be recorded as 0.");
                return 0;
            }
        }
    }
}
#endif
