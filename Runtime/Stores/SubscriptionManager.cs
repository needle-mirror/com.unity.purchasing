using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Use to query in-app purchasing subscription product information, and upgrade subscription products.
    /// Supports the Apple App Store, Google Play store.
    /// Note expiration dates may become invalid after updating subscriptions between two types of duration.
    /// </summary>
    /// <seealso cref="IAppleExtensions.GetIntroductoryPriceDictionary"/>
    /// <seealso cref="UpdateSubscription"/>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class SubscriptionManager : SubscriptionInfoHelper
    {
        /// <summary>
        /// Performs subscription updating, migrating a subscription into another as long as they are both members
        /// of the same subscription group on the App Store.
        /// </summary>
        /// <param name="newProduct">Destination subscription product, belonging to the same subscription group as <paramref name="oldProduct"/></param>
        /// <param name="oldProduct">Source subscription product, belonging to the same subscription group as <paramref name="newProduct"/></param>
        /// <param name="developerPayload">Carried-over metadata from prior call to <typeparamref name="SubscriptionManager.UpdateSubscription"/> </param>
        /// <param name="appleStore">Triggered upon completion of the subscription update.</param>
        /// <param name="googleStore">Triggered upon completion of the subscription update.</param>
        public static void UpdateSubscription(Product newProduct, Product oldProduct, string developerPayload, Action<Product, string> appleStore, Action<string, string> googleStore)
        {
            if (oldProduct.receipt == null)
            {
                Debug.unityLogger.LogIAPError("The product has not been purchased, a subscription can only " +
                    "be upgrade/downgrade when has already been purchased");
                return;
            }
            var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(oldProduct.receipt);
            if (receipt_wrapper == null || !receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload"))
            {
                Debug.unityLogger.LogIAPWarning("The product receipt does not contain enough information");
                return;
            }
            var store = (string)receipt_wrapper["Store"];
            var payload = (string)receipt_wrapper["Payload"];

            if (payload != null)
            {
                switch (store)
                {
                    case "GooglePlay":
                    {
                        var oldSubscriptionManager = new SubscriptionManager(oldProduct, null);
                        SubscriptionInfo oldSubscriptionInfo; ;
                        try
                        {
                            oldSubscriptionInfo = oldSubscriptionManager.getSubscriptionInfo();
                        }
                        catch (Exception e)
                        {
                            Debug.unityLogger.LogIAPError($"The product that will be updated does not have a " +
                                $"valid receipt: {e}");
                            return;
                        }
                        var newSubscriptionId = newProduct.definition.storeSpecificId;
                        googleStore(oldSubscriptionInfo.GetSubscriptionInfoJsonString(), newSubscriptionId);
                        return;
                    }
                    case "AppleAppStore":
                    case "MacAppStore":
                    {
                        appleStore(newProduct, developerPayload);
                        return;
                    }
                    default:
                    {
                        Debug.unityLogger.LogIAPWarning("This store does not support update subscriptions");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Performs subscription updating, migrating a subscription into another as long as they are both members
        /// of the same subscription group on the App Store.
        /// </summary>
        /// <param name="oldProduct">Source subscription product, belonging to the same subscription group as <paramref name="newProduct"/></param>
        /// <param name="newProduct">Destination subscription product, belonging to the same subscription group as <paramref name="oldProduct"/></param>
        /// <param name="googlePlayUpdateCallback">Triggered upon completion of the subscription update.</param>
        public static void UpdateSubscriptionInGooglePlayStore(Product oldProduct, Product newProduct, Action<string, string> googlePlayUpdateCallback)
        {
            var oldSubscriptionManager = new SubscriptionManager(oldProduct, null);
            SubscriptionInfo oldSubscriptionInfo;
            try
            {
                oldSubscriptionInfo = oldSubscriptionManager.getSubscriptionInfo();
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPError($"The product that will be updated does not have a valid " +
                    $"receipt: {e}");
                return;
            }
            var newSubscriptionId = newProduct.definition.storeSpecificId;
            googlePlayUpdateCallback(oldSubscriptionInfo.GetSubscriptionInfoJsonString(), newSubscriptionId);
        }

        /// <summary>
        /// Performs subscription updating, migrating a subscription into another as long as they are both members
        /// of the same subscription group on the App Store.
        /// </summary>
        /// <param name="newProduct">Destination subscription product, belonging to the same subscription group as <paramref name="oldProduct"/></param>
        /// <param name="developerPayload">Carried-over metadata from prior call to <typeparamref name="SubscriptionManager.UpdateSubscription"/> </param>
        /// <param name="appleStoreUpdateCallback">Triggered upon completion of the subscription update.</param>
        public static void UpdateSubscriptionInAppleStore(Product newProduct, string developerPayload, Action<Product, string> appleStoreUpdateCallback)
        {
            appleStoreUpdateCallback(newProduct, developerPayload);
        }

        /// <summary>
        /// Construct an object that allows inspection of a subscription product.
        /// </summary>
        /// <param name="product">Subscription to be inspected</param>
        /// <param name="intro_json">From <typeparamref name="IAppleExtensions.GetIntroductoryPriceDictionary"/></param>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public SubscriptionManager(Product product, string intro_json)
            : base(product, intro_json)
        {
        }

        /// <summary>
        /// Construct an object that allows inspection of a subscription product.
        /// </summary>
        /// <param name="receipt">A Unity IAP unified receipt from <typeparamref name="Product.receipt"/></param>
        /// <param name="id">A product identifier.</param>
        /// <param name="intro_json">From <typeparamref name="IAppleExtensions.GetIntroductoryPriceDictionary"/></param>
        public SubscriptionManager(string receipt, string id, string intro_json)
            : base(receipt, id, intro_json)
        {
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
        public SubscriptionInfo getSubscriptionInfo()
        {
            return GetSubscriptionInfo();
        }
    }
}
