using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{

    /// <summary>
    /// A period of time expressed in either days, months, or years. Conveys a subscription's duration definition.
    /// Note this reflects the types of subscription durations settable on a subscription on supported app stores.
    /// </summary>
    public class TimeSpanUnits
    {
        /// <summary>
        /// Discrete duration in days, if less than a month, otherwise zero.
        /// </summary>
        public double days;
        /// <summary>
        /// Discrete duration in months, if less than a year, otherwise zero.
        /// </summary>
        public int months;
        /// <summary>
        /// Discrete duration in years, otherwise zero.
        /// </summary>
        public int years;

        /// <summary>
        /// Construct a subscription duration.
        /// </summary>
        /// <param name="d">Discrete duration in days, if less than a month, otherwise zero.</param>
        /// <param name="m">Discrete duration in months, if less than a year, otherwise zero.</param>
        /// <param name="y">Discrete duration in years, otherwise zero.</param>
        public TimeSpanUnits(double d, int m, int y)
        {
            days = d;
            months = m;
            years = y;
        }
    }

    /// <summary>
    /// Use to query in-app purchasing subscription product information, and upgrade subscription products.
    /// Supports the Apple App Store, Google Play store.
    /// Note expiration dates may become invalid after updating subscriptions between two types of duration.
    /// </summary>
    /// <seealso cref="IAppleExtensions.GetIntroductoryPriceDictionary"/>
    /// <seealso cref="UpdateSubscription"/>
    public class SubscriptionInfoHelper
    {
        readonly string m_Receipt;
        readonly string m_ProductId;
        readonly string m_IntroJson;

        /// <summary>
        /// Construct an object that allows inspection of a subscription product.
        /// </summary>
        /// <param name="product">Subscription to be inspected</param>
        /// <param name="introJson">From <typeparamref name="IAppleStoreExtendedProductService.GetIntroductoryPriceDictionary"/></param>
        public SubscriptionInfoHelper(Product product, string introJson)
        {
            m_Receipt = product.receipt;
            m_ProductId = product.definition.storeSpecificId;
            m_IntroJson = introJson;
        }

        /// <summary>
        /// Construct an object that allows inspection of a subscription product.
        /// </summary>
        /// <param name="receipt">A Unity IAP unified receipt from <typeparamref name="Product.receipt"/></param>
        /// <param name="id">A product identifier.</param>
        /// <param name="introJson">From <typeparamref name="IAppleStoreExtendedProductService.GetIntroductoryPriceDictionary"/></param>
        public SubscriptionInfoHelper(string receipt, string id, string introJson)
        {
            m_Receipt = receipt;
            m_ProductId = id;
            m_IntroJson = introJson;
        }

        /// <summary>
        /// Convert my Product into a <typeparamref name="SubscriptionInfo"/>.
        /// My Product.receipt must have a "Payload" JSON key containing supported native app store
        /// information, which will be converted here.
        /// </summary>
        /// <returns>Returns the SubscriptionInfo</returns>
        /// <exception cref="NullProductIdException">My Product must have a non-null product identifier</exception>
        /// <exception cref="StoreSubscriptionInfoNotSupportedException">A supported app store must be used as my product</exception>
        /// <exception cref="NullReceiptException">My product must have a receipt</exception>
        /// <exception cref="InvalidProductTypeException">For non-subscription product types</exception>
        public SubscriptionInfo GetSubscriptionInfo()
        {
            if (m_Receipt != null)
            {
                var receiptWrapper = (Dictionary<string, object>)MiniJson.JsonDecode(m_Receipt);

                var validPayload = receiptWrapper.TryGetValue("Payload", out var payloadAsObject);
                var validStore = receiptWrapper.TryGetValue("Store", out var storeAsObject);

                if (validPayload && validStore)
                {

                    var payload = payloadAsObject as string;
                    var store = storeAsObject as string;

                    switch (store)
                    {
                        case GooglePlay.Name:
                        {
                            return GetGooglePlayStoreSubInfo(payload);
                        }
                        case AppleAppStore.Name:
                        case MacAppStore.Name:
                        {
                            if (m_ProductId == null)
                            {
                                throw new NullProductIdException();
                            }
                            return GetAppleAppStoreSubInfo(payload, m_ProductId);
                        }
                        default:
                        {
                            throw new StoreSubscriptionInfoNotSupportedException("Store not supported: " + store);
                        }
                    }
                }
            }

            throw new NullReceiptException();
        }

        SubscriptionInfo GetAppleAppStoreSubInfo(string payload, string productId)
        {
            AppleReceipt receipt = null;

            var logger = Debug.unityLogger;

            try
            {
                if (string.Empty.Equals(payload))
                {
                    logger.LogIAP("Apple receipt is empty. Call `IAppleStoreExtendedPurchaseService.RefreshAppReceipt` to refresh it.");
                }
                else
                {
                    receipt = new AppleReceiptParser().Parse(Convert.FromBase64String(payload));
                }
            }
            catch (ArgumentException e)
            {
                logger.LogIAP($"Unable to parse Apple receipt: {e}");
            }
            catch (IAPSecurityException e)
            {
                logger.LogIAP($"Unable to parse Apple receipt: {e}");
            }
            catch (NullReferenceException e)
            {
                logger.LogIAP($"Unable to parse Apple receipt: {e}");
            }

            var inAppPurchaseReceipts = new List<AppleInAppPurchaseReceipt>();

            if (receipt?.inAppPurchaseReceipts != null && receipt.inAppPurchaseReceipts.Length > 0)
            {
                foreach (var r in receipt.inAppPurchaseReceipts)
                {
                    if (r.productID.Equals(productId))
                    {
                        inAppPurchaseReceipts.Add(r);
                    }
                }
            }
            return inAppPurchaseReceipts.Count == 0 ? null : new SubscriptionInfo(FindMostRecentReceipt(inAppPurchaseReceipts), m_IntroJson);
        }

        static AppleInAppPurchaseReceipt FindMostRecentReceipt(List<AppleInAppPurchaseReceipt> receipts)
        {
            receipts.Sort((b, a) => a.purchaseDate.CompareTo(b.purchaseDate));
            return receipts[0];
        }

        static SubscriptionInfo GetGooglePlayStoreSubInfo(string payload)
        {
            var payloadWrapper = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
            payloadWrapper.TryGetValue("skuDetails", out var skuDetailsObject);

            var skuDetails = (skuDetailsObject as List<object>)?.Select(obj => obj as string);

            var originalJsonPayloadWrapper =
                (Dictionary<string, object>)MiniJson.JsonDecode((string)payloadWrapper["json"]);

            var validIsAutoRenewingKey =
                originalJsonPayloadWrapper.TryGetValue("autoRenewing", out var autoRenewingObject);

            var isAutoRenewing = false;
            if (validIsAutoRenewingKey)
            {
                isAutoRenewing = (bool)autoRenewingObject;
            }

            // Google specifies times in milliseconds since 1970.
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var validPurchaseTimeKey =
                originalJsonPayloadWrapper.TryGetValue("purchaseTime", out var purchaseTimeObject);

            long purchaseTime = 0;

            if (validPurchaseTimeKey)
            {
                purchaseTime = (long)purchaseTimeObject;
            }

            var purchaseDate = epoch.AddMilliseconds(purchaseTime);

            var validDeveloperPayloadKey =
                originalJsonPayloadWrapper.TryGetValue("developerPayload", out var developerPayloadObject);

            var isFreeTrial = false;
            var hasIntroductoryPrice = false;
            string updateMetadata = null;

            if (validDeveloperPayloadKey)
            {
                var developerPayloadJSON = (string)developerPayloadObject;
                var developerPayloadWrapper = (Dictionary<string, object>)MiniJson.JsonDecode(developerPayloadJSON);
                var validIsFreeTrialKey =
                    developerPayloadWrapper.TryGetValue("is_free_trial", out var isFreeTrialObject);
                if (validIsFreeTrialKey)
                {
                    isFreeTrial = (bool)isFreeTrialObject;
                }

                var validHasIntroductoryPriceKey =
                    developerPayloadWrapper.TryGetValue("has_introductory_price_trial",
                        out var hasIntroductoryPriceObject);

                if (validHasIntroductoryPriceKey)
                {
                    hasIntroductoryPrice = (bool)hasIntroductoryPriceObject;
                }

                var validIsUpdatedKey = developerPayloadWrapper.TryGetValue("is_updated", out var isUpdatedObject);

                var isUpdated = false;

                if (validIsUpdatedKey)
                {
                    isUpdated = (bool)isUpdatedObject;
                }

                if (isUpdated)
                {
                    var isValidUpdateMetaKey = developerPayloadWrapper.TryGetValue("update_subscription_metadata",
                        out var updateMetadataObject);

                    if (isValidUpdateMetaKey)
                    {
                        updateMetadata = (string)updateMetadataObject;
                    }
                }
            }

            var skuDetail = skuDetails?.First();

            return new SubscriptionInfo(skuDetail, isAutoRenewing, purchaseDate, isFreeTrial, hasIntroductoryPrice,
                false, updateMetadata);
        }
    }

    /// <summary>
    /// Error found during receipt parsing.
    /// </summary>
    [Serializable]
    public class ReceiptParserException : IapException
    {
        /// <summary>
        /// Construct an error object for receipt parsing.
        /// </summary>
        public ReceiptParserException() { }

        /// <summary>
        /// Construct an error object for receipt parsing.
        /// </summary>
        /// <param name="message">Description of error</param>
        public ReceiptParserException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the ReceiptParserException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ReceiptParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// An error was found when an invalid <typeparamref name="Product.definition.type"/> is provided.
    /// </summary>
    [Serializable]
    public class InvalidProductTypeException : ReceiptParserException
    {
        /// <summary>
        /// Construct an error object for receipt parsing an invalid product type.
        /// </summary>
        public InvalidProductTypeException() { }

        /// <summary>
        /// Construct an error object for receipt parsing an invalid product type.
        /// </summary>
        /// <param name="message">Description of error</param>
        public InvalidProductTypeException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the InvalidProductTypeException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InvalidProductTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// An error was found when an unexpectedly null <typeparamref name="Product.definition.id"/> is provided.
    /// </summary>
    [Serializable]
    public class NullProductIdException : ReceiptParserException
    {
        /// <summary>
        /// Construct an error object for receipt parsing a null product id.
        /// </summary>
        public NullProductIdException() { }

        /// <summary>
        /// Construct an error object for receipt parsing a null product id.
        /// </summary>
        /// <param name="message">Description of error</param>
        public NullProductIdException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the NullProductIdException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected NullProductIdException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// An error was found when an unexpectedly null <typeparamref name="Product.receipt"/> is provided.
    /// </summary>
    [Serializable]
    public class NullReceiptException : ReceiptParserException
    {
        /// <summary>
        /// Construct an error object for parsing a null receipt.
        /// </summary>
        public NullReceiptException() { }

        /// <summary>
        /// Construct an error object for parsing a null receipt.
        /// </summary>
        /// <param name="message">Description of error</param>
        public NullReceiptException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the NullReceiptException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected NullReceiptException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// An error was found when an unsupported app store <typeparamref name="Product.receipt"/> is provided.
    /// </summary>
    [Serializable]
    public class StoreSubscriptionInfoNotSupportedException : ReceiptParserException
    {
        /// <summary>
        /// An error was found when an unsupported app store <typeparamref name="Product.receipt"/> is provided.
        /// </summary>
        /// <param name="message">Human readable explanation of this error</param>
        public StoreSubscriptionInfoNotSupportedException(string message) : base(message)
        {
        }
    }
}
