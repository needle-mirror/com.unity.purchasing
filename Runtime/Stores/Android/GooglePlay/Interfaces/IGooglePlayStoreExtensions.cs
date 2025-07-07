using System;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access GooglePlay store specific functionality.
    /// </summary>
    public interface IGooglePlayStoreExtensions : IStoreExtension
    {
        /// <summary>
        /// Upgrade or downgrade subscriptions, with proration mode `IMMEDIATE_WITHOUT_PRORATION` by default
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
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
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </param>
        void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode);

        /// <summary>
        /// Upgrade or downgrade subscriptions
        /// </summary>
        /// <param name="oldSku">current subscription</param>
        /// <param name="newSku">new subscription to subscribe</param>
        /// <param name="desiredProrationMode">Specifies the mode of proration.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </param>
        [Obsolete("Use UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayReplacementMode desiredReplacementMode) instead.")]
        void UpgradeDowngradeSubscription(string oldSku, string newSku, GooglePlayProrationMode desiredProrationMode);

        /// <summary>
        /// Upgrade or downgrade subscriptions
        /// </summary>
        /// <param name="oldSku">current subscription</param>
        /// <param name="newSku">new subscription to subscribe</param>
        /// <param name="desiredReplacementMode">Specifies the mode of proration.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </param>
        void UpgradeDowngradeSubscription(string oldSku, string newSku, GooglePlayReplacementMode desiredReplacementMode);

        /// <summary>
        /// Async call to the google store to <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient#querypurchases">`queryPurchases`</a>
        /// using all the different support sku types.
        /// </summary>
        /// <param name="callback">Will be called as often as many purchases the queryPurchases finds. (the IStoreCallback will be called as well)</param>
        [Obsolete("RestoreTransactions(Action<bool> callback) is deprecated, please use RestoreTransactions(Action<bool, string> callback) instead.")]
        void RestoreTransactions(Action<bool> callback);

        /// <summary>
        /// Async call to the google store to <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient#querypurchases">`queryPurchases`</a>
        /// using all the different support sku types.
        /// </summary>
        /// <param name="callback">Will be called as often as many purchases the queryPurchases finds (the IStoreCallback will be called as well). The bool will be true if it was successful with a null string or false if it was not with the error message in the string.</param>
        void RestoreTransactions(Action<bool, string> callback);

        /// <summary>
        /// Initiate a flow to confirm the change of price for an item subscribed by the user.
        /// </summary>
        /// <param name="productId">Product id</param>
        /// <param name="callback">Price changed event finished successfully</param>
        [Obsolete("No longer functional. This calls launchPriceChangeConfirmationFlow which was removed from Google Play Billing Library 6.0.0")]
        void ConfirmSubscriptionPriceChange(string productId, Action<bool> callback);

        /// <summary>
        /// Determines if the purchase of a product in the Google Play Store is deferred based on its receipt. This indicates if there is an additional step to complete a transaction in between when a user initiates a purchase and when the payment method for the purchase is processed.
        /// <a href="https://developer.android.com/google/play/billing/integrate#pending">Handling pending transactions</a>
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns><c>true</c>if the input contains a receipt for a deferred or a pending transaction for a Google Play billing purchase, and <c>false</c> otherwise.</returns>
        bool IsPurchasedProductDeferred(Product product);

        /// <summary>
        /// Returns the purchase state of a product in the Google Play Store.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getPurchaseState()">getPurchaseState</a>
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Returns the purchase state when successful, otherwise an exception is thrown.</returns>
        GooglePurchaseState GetPurchaseState(Product product);

        /// <summary>
        /// Returns the obfuscated account id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedAccountId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Returns the obfuscated account id if it exists, otherwise null is returned.</returns>
        string GetObfuscatedAccountId(Product product);

        /// <summary>
        /// Returns the obfuscated profile id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedProfileId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Returns the obfuscated profile id if it exists, otherwise null is returned.</returns>
        string GetObfuscatedProfileId(Product product);
    }
}
