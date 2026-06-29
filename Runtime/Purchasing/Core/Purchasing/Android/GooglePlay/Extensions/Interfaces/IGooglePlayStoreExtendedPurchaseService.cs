#nullable enable
using System;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Google Play Store purchase service extension.
    /// </summary>
    public interface IGooglePlayStoreExtendedPurchaseService : IPurchaseServiceExtension
    {
        /// <summary>
        /// Upgrade or downgrade subscriptions, with proration mode `IMMEDIATE_WITHOUT_PRORATION` by default
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </summary>
        /// <param name="oldProduct">current subscription</param>
        /// <param name="newProduct">new subscription to subscribe</param>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct);

        /// <summary>
        /// Upgrade or downgrade subscriptions
        /// </summary>
        /// <param name="oldProduct">current subscription</param>
        /// <param name="newProduct">new subscription to subscribe</param>
        /// <param name="desiredProrationMode">Specifies the mode of proration.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
		/// </param>
		[Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayProrationMode desiredProrationMode);

        /// <summary>
        /// Upgrade or downgrade subscriptions
        /// </summary>
        /// <param name="oldProduct">current subscription</param>
        /// <param name="newProduct">new subscription to subscribe</param>
        /// <param name="desiredReplacementMode">Specifies the replacement mode.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
		/// </param>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayReplacementMode desiredReplacementMode);

        /// <summary>
        /// Upgrade or downgrade subscriptions.
        /// </summary>
        /// <param name="currentOrder">current order associated with the subscription</param>
        /// <param name="newProduct">new subscription to subscribe</param>
        /// <param name="desiredReplacementMode">Specifies the replacement mode.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </param>
        void UpgradeDowngradeSubscription(Order currentOrder, Product newProduct, GooglePlayReplacementMode desiredReplacementMode);

        /// <summary>
        /// Convenience wrapper for <see cref="UpgradeDowngradeSubscription(Order, Product, GooglePlayReplacementMode)"/>
        /// that resolves the new subscription product from its catalog listing id. Looks up the
        /// product via <c>productCache.FindByCatalogListingId(newCatalogListingId)</c>; falls back
        /// to an unknown product (which the underlying call will fail) if no match is found.
        /// Prefer the <see cref="Product"/> overload when you already hold a Product reference.
        /// </summary>
        /// <param name="currentOrder">current order associated with the subscription</param>
        /// <param name="newCatalogListingId">catalog listing id of the new subscription to subscribe</param>
        /// <param name="desiredReplacementMode">Specifies the replacement mode.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingFlowParams.SubscriptionUpdateParams.ReplacementMode">See more</a>
        /// </param>
        void UpgradeDowngradeSubscription(Order currentOrder, string newCatalogListingId, GooglePlayReplacementMode desiredReplacementMode);

        /// <summary>
        /// Determines if the purchase of a product in the Google Play Store is deferred based on its receipt. This indicates if there is an additional step to complete a transaction in between when a user initiates a purchase and when the payment method for the purchase is processed.
        /// <a href="https://developer.android.com/google/play/billing/integrate#pending">Handling pending transactions</a>
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns><c>true</c>if the input contains a receipt for a deferred or a pending transaction for a Google Play billing purchase, and <c>false</c> otherwise.</returns>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        bool IsOrderDeferred(Order order);

        /// <summary>
        /// Returns the purchase state of a product in the Google Play Store.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getPurchaseState()">getPurchaseState</a>
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Returns the purchase state when successful, otherwise null is returned.</returns>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        GooglePurchaseState? GetPurchaseState(Order order);

        /// <summary>
        /// Returns the obfuscated account id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedAccountId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Returns the obfuscated account id if it exists, otherwise null is returned.</returns>
        string? GetObfuscatedAccountId(Order order);

        /// <summary>
        /// Returns the obfuscated profile id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedProfileId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Returns the obfuscated profile id if it exists, otherwise null is returned.</returns>
        string? GetObfuscatedProfileId(Order order);

        /// <summary>
        /// Set listener for deferred subscription change events.
        /// Deferred subscription changes only take effect at the renewal cycle and no transaction is done immediately, therefore there is no receipt nor token.
        /// No payout is granted here. Instead, notify the user that the subscription change will take effect at the next renewal cycle.
        /// </summary>
        event Action<DeferredPaymentUntilRenewalDateOrder> OnDeferredPaymentUntilRenewalDate;
    }
}
