#if IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
#nullable enable

using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Internal;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class CoreAnalyticsAdapter : IAnalyticsAdapter
    {
        readonly IAnalyticsService m_Analytics;
        readonly ILogger m_Logger;
        IAnalyticsStandardEventComponent? m_CoreAnalytics;
        const string k_TransactionEventName = "transaction";
        const string k_TransactionFailedEventName = "transactionFailed";
        const string k_PurchasingPackageName = "com.unity.purchasing";
        const int k_TransactionEventVersion = 1;
        const int k_TransactionFailedEventVersion = 1;

        public CoreAnalyticsAdapter(IAnalyticsService analytics, ILogger logger)
        {
            m_Analytics = analytics;
            m_Logger = logger;
        }

        public void SendTransactionEvent(Product product)
        {
            CoreAnalytics()?.Record(k_TransactionEventName,
                BuildTransactionParameters(product),
                k_TransactionEventVersion,
                k_PurchasingPackageName);
        }

        Dictionary<string, object?> BuildTransactionParameters(Product product)
        {
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(product.receipt);
            var analyticsReceipt = unifiedReceipt.ToReceiptAndSignature();
            return new Dictionary<string, object?>
            {
                { "transactionID", unifiedReceipt.TransactionID },
                { "transactionName", GetTransactionName(product) },
                { "transactionReceipt", analyticsReceipt.transactionReceipt },
                { "transactionReceiptSignature", analyticsReceipt.transactionReceiptSignature },
                { "transactionServer", analyticsReceipt.transactionServer },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", product.definition.storeSpecificId },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(product) },
                { "productsReceived", GenerateItemReceivedForPurchase(product) }
            };
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureDescription description)
        {
            CoreAnalytics()?.Record(k_TransactionFailedEventName,
                BuildTransactionFailedParameters(product, description.reason),
                k_TransactionFailedEventVersion,
                k_PurchasingPackageName);
        }

        IAnalyticsStandardEventComponent? CoreAnalytics()
        {
            try
            {
                return m_CoreAnalytics ??= CoreRegistry.Instance.GetServiceComponent<IAnalyticsStandardEventComponent>();
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        Dictionary<string, object> BuildTransactionFailedParameters(Product product,
            PurchaseFailureReason reason)
        {
            return new Dictionary<string, object>
            {
                { "transactionName", GetTransactionName(product) },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", product.definition.storeSpecificId },
                { "failureReason", reason.ToString() },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(product) },
                { "productsReceived", GenerateItemReceivedForPurchase(product) }
            };
        }

        static string GetTransactionName(Product product)
        {
            return string.IsNullOrEmpty(product.metadata.localizedTitle) ?
                product.definition.storeSpecificId :
                product.metadata.localizedTitle;
        }

        static Dictionary<string, object> GenerateItemReceivedForPurchase(Product product)
        {
            return new Dictionary<string, object>
            {
                { "items", new List<object>
                    {
                        new Dictionary<string, object> {
                            { "item", new Dictionary<string, object>
                                {
                                    { "itemName", product.definition.id },
                                    { "itemAmount", 1 },
                                    { "itemType", product.definition.type.ToString() }
                                }
                            }
                        }
                    }
                }
            };
        }

        Dictionary<string, object> GenerateRealCurrencySpentOnPurchase(Product product)
        {
            return new Dictionary<string, object>
            {
                { "realCurrency", CreateRealCurrencyFromProduct(product) }
            };
        }

        Dictionary<string, object> CreateRealCurrencyFromProduct(Product product)
        {
            return new Dictionary<string, object>
            {
                { "realCurrencyAmount", CheckCurrencyCodeAndExtractRealCurrencyAmount(product) },
                { "realCurrencyType", product.metadata.isoCurrencyCode }
            };
        }

        long CheckCurrencyCodeAndExtractRealCurrencyAmount(Product product)
        {
            if (product.metadata.isoCurrencyCode != null)
            {
                return ExtractRealCurrencyAmount(product);
            }

            m_Logger.LogIAPWarning($"The isoCurrencyCode for product ID {product.definition.id} is null. Were you trying to purchase an unavailable product? The price will be recorded as 0.");
            return 0;
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
