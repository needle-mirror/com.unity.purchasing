#if IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT

#nullable enable

using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Internal;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class CoreAnalyticsAdapter : IAnalyticsAdapter
    {
        readonly ILogger m_Logger;
        IAnalyticsStandardEventComponent? m_CoreAnalytics;
        const string k_TransactionEventName = "transaction";
        const string k_TransactionFailedEventName = "transactionFailed";
        const string k_PurchasingPackageName = "com.unity.purchasing";
        const int k_TransactionEventVersion = 1;
        const int k_TransactionFailedEventVersion = 1;

        TransactionEventHelper m_TransactionEventHelper;

        [Preserve]
        internal CoreAnalyticsAdapter(IAnalyticsServiceWrapper analytics, ILogger logger)
        {
            m_Logger = logger;
            m_TransactionEventHelper = new TransactionEventHelper(analytics, logger);
        }

        public void SendTransactionEvent(CartItem item, string receipt)
        {
            CoreAnalyticsComponent()?.Record(k_TransactionEventName,
                BuildTransactionParameters(item, receipt),
                k_TransactionEventVersion,
                k_PurchasingPackageName);
        }

        Dictionary<string, object?> BuildTransactionParameters(CartItem item, string receipt)
        {
            var unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(receipt);
            var analyticsReceipt = unifiedReceipt.ToReceiptAndSignature();
            return new Dictionary<string, object?>
            {
                { "transactionID", unifiedReceipt.TransactionID },
                { "transactionName", CommonTransactionEventHelper.GetTransactionName(item.Product) },
                { "transactionReceipt", analyticsReceipt.transactionReceipt },
                { "transactionReceiptSignature", analyticsReceipt.transactionReceiptSignature },
                { "transactionServer", analyticsReceipt.transactionServer },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", item.Product.definition.storeSpecificId },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(item.Product) },
                { "productsReceived", GenerateItemReceivedForPurchase(item) }
            };
        }

        public void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription)
        {
            CoreAnalyticsComponent()?.Record(k_TransactionFailedEventName,
                BuildTransactionFailedParameters(failureDescription.product, TransactionFailedEventHelper.BuildFailureReason(failureDescription)),
                k_TransactionFailedEventVersion,
                k_PurchasingPackageName);
        }

        IAnalyticsStandardEventComponent? CoreAnalyticsComponent()
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

        Dictionary<string, object> BuildTransactionFailedParameters(CartItem item,
            string failureReason)
        {
            return new Dictionary<string, object>
            {
                { "transactionName", CommonTransactionEventHelper.GetTransactionName(item.Product) },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", item.Product.definition.storeSpecificId },
                { "failureReason", failureReason },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(item.Product) },
                { "productsReceived", GenerateItemReceivedForPurchase(item) }
            };
        }

        static Dictionary<string, object> GenerateItemReceivedForPurchase(CartItem item)
        {
            return new Dictionary<string, object>
            {
                { "items", new List<object>
                    {
                        new Dictionary<string, object> {
                            { "item", new Dictionary<string, object>
                                {
                                    { "itemName", item.Product.definition.id },
                                    { "itemAmount", item.Quantity },
                                    { "itemType", item.Product.definition.type.ToString() }
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
                { "realCurrencyAmount", m_TransactionEventHelper.CheckCurrencyCodeAndExtractRealCurrencyAmount(product) },
                { "realCurrencyType", product.metadata.isoCurrencyCode ?? "" }
            };
        }
    }
}

#endif
