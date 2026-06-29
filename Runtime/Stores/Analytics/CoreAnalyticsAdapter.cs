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
            item.Product.catalogListings.TryGetValue(item.CatalogListingId, out var listing);
            var transactionParameters = new Dictionary<string, object?>
            {
                { "transactionID", unifiedReceipt.TransactionID },
                { "transactionName", CommonTransactionEventHelper.GetTransactionName(listing) },
                { "transactionReceipt", analyticsReceipt.transactionReceipt },
                { "transactionReceiptSignature", analyticsReceipt.transactionReceiptSignature },
                { "transactionServer", analyticsReceipt.transactionServer },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", listing?.definition?.storeSpecificId },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(listing) },
                { "productsReceived", GenerateItemReceivedForPurchase(item) }
            };

            return transactionParameters;
        }

        static bool IsAppleAppStore()
        {
            return
                Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer ||
#if UNITY_VISIONOS
                Application.platform == RuntimePlatform.VisionOS ||
#endif
                Application.platform == RuntimePlatform.tvOS;
        }

        public void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription)
        {
            CoreAnalyticsComponent()?.Record(k_TransactionFailedEventName,
                BuildTransactionFailedParameters(failureDescription.item, TransactionFailedEventHelper.BuildFailureReason(failureDescription)),
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

        Dictionary<string, object?> BuildTransactionFailedParameters(CartItem item,
            string failureReason)
        {
            // Listing may have been removed since the cart was built (e.g. user logged out).
            item.Product.catalogListings.TryGetValue(item.CatalogListingId, out var listing);
            return new Dictionary<string, object?>
            {
                { "transactionName", CommonTransactionEventHelper.GetTransactionName(listing) },
                { "transactionType", TransactionType.PURCHASE },
                { "productID", listing?.definition?.storeSpecificId },
                { "failureReason", failureReason },
                { "productsSpent", GenerateRealCurrencySpentOnPurchase(listing) },
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
                                    { "itemName", item.Product.uSku },
                                    { "itemAmount", item.Quantity },
                                    { "itemType", item.Product.type.ToString() }
                                }
                            }
                        }
                    }
                }
            };
        }

        Dictionary<string, object> GenerateRealCurrencySpentOnPurchase(CatalogListing? listing)
        {
            return new Dictionary<string, object>
            {
                { "realCurrency", CreateRealCurrencyFromListing(listing) }
            };
        }

        Dictionary<string, object> CreateRealCurrencyFromListing(CatalogListing? listing)
        {
            return new Dictionary<string, object>
            {
                { "realCurrencyAmount", m_TransactionEventHelper.CheckCurrencyCodeAndExtractRealCurrencyAmount(listing) },
                { "realCurrencyType", listing?.metadata?.isoCurrencyCode ?? "" }
            };
        }
    }
}

#endif
