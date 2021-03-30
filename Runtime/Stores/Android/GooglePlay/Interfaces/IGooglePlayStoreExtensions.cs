using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
	/// <summary>
	/// Access GooglePlay store specific functionality.
	/// </summary>
	public interface IGooglePlayStoreExtensions : IStoreExtension
    {
        /// <summary>
        /// Upgrade or downgrade subscriptions, with proration mode `IMMEDIATE_WITHOUT_PRORATION` by default
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.ProrationMode">See more</a>
        /// </summary>
        /// <param name="oldSku">current subscription</param>
        /// <param name="newSku">new subscription to subscribe</param>
        void UpgradeDowngradeSubscription(string oldSku, string newSku);

        /// <summary>
        /// Upgrade or downgrade subscriptions
        /// </summary>
        /// <param name="oldSku">current subscription</param>
        /// <param name="newSku">new subscription to subscribe</param>
        /// <param name="desiredProrationMode">Specifies the mode of proration.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.ProrationMode">See more</a>
        /// </param>
        void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode);

        /// <summary>
        /// Async call to the google store to <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient#querypurchases">`queryPurchases`</a>
        /// using all the different support sku types.
        /// </summary>
        /// <param name="callback">Will be called as often as many purchases the queryPurchases finds. (the IStoreCallback will be called as well)</param>
        void RestoreTransactions(Action<bool> callback);

        /// <summary>
        /// Executes the same code as `GooglePlayStore.FinishTransaction`.
        /// </summary>
        /// <param name="productId">Products id / sku</param>
        /// <param name="transactionId">Products transaction id</param>
        [Obsolete("FinishAdditionalTransaction is deprecated, please use GooglePlayStore.FinishTransaction instead. Will be removed soon...")]
        void FinishAdditionalTransaction(string productId, string transactionId);

        /// <summary>
        /// Initiate a flow to confirm the change of price for an item subscribed by the user.
        /// </summary>
        /// <param name="productId">Product id</param>
        /// <param name="callback">Price changed event finished successfully</param>
        void ConfirmSubscriptionPriceChange(string productId, Action<bool> callback);

        /// <summary>
        /// Set listener for deferred purchasing events.
        /// Deferred purchasing is enabled by default and cannot be changed.
        /// </summary>
        /// <param name="action">Deferred purchasing successful events. Do not grant the item here. Instead, record the purchase and remind the user to complete the transaction in the Play Store. </param>
        void SetDeferredPurchaseListener(Action<Product> action);

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase.
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="accountId">The obfuscated account id</param>
        void SetObfuscatedAccountId(string accountId);

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="profileId">The obfuscated profile id</param>
        void SetObfuscatedProfileId(string profileId);

        /// <summary>
        /// GetProductJSONDictionary is deprecated, nothing will be returns and no code will be executed. Will be removed soon. Use the `GoogleProductMetadata` of `product.metadata.GetGoogleProductMetadata()` from `IStoreController.products`
        /// </summary>
        /// <returns>null</returns>
        [Obsolete("GetProductJSONDictionary is deprecated, nothing will be returns and no code will be executed. Will be removed soon. Use the `GoogleProductMetadata` of `product.metadata.GetGoogleProductMetadata()` from `IStoreController.products`")]
        Dictionary<string, string> GetProductJSONDictionary();

        /// <summary>
        /// SetLogLevel is deprecated, no code will be executed. Will be removed soon.
        /// </summary>
        /// <param name="level">deprecated</param>
        [Obsolete("SetLogLevel is deprecated, no code will be executed. Will be removed soon.")]
        void SetLogLevel(int level);

        /// <summary>
        /// IsOwned is deprecated, false will be returned by default and no code will be executed. Will be removed soon.
        /// </summary>
        /// <param name="p">deprecated</param>
        /// <returns>false</returns>
        [Obsolete("IsOwned is deprecated, false will be returned by default and no code will be executed. Will be removed soon.")]
        bool IsOwned(Product p);
    }
}
