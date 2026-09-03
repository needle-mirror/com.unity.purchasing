#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about a Google order.
    /// </summary>
    public interface IGoogleOrderInfo
    {
        /// <summary>
        /// The obfuscated account id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedAccountId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <value>Returns the obfuscated account id if it exists, otherwise null is returned.</value>
        string? ObfuscatedAccountId { get; set; }

        /// <summary>
        /// The obfuscated profile id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedProfileId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <value>Returns the obfuscated profile id if it exists, otherwise null is returned.</value>
        string? ObfuscatedProfileId { get; set; }

        /// <summary>
        /// The Google Play order id of the purchase.
        /// For subscription renewals, this is the order id of the initial order — renewal order ids (suffixed <c>..0</c>, <c>..1</c>, …) are not surfaced.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getOrderId()">getOrderId</a>
        /// </summary>
        /// <value>Returns the order id if it exists, otherwise null is returned.</value>
        string? OrderId { get; }

        /// <summary>
        /// The Google Play purchase token of the purchase.
        /// On Google Play, <typeparamref name="IOrderInfo.TransactionID"/> contains the purchase token; this property returns the same value under its Google name.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getPurchaseToken()">getPurchaseToken</a>
        /// </summary>
        /// <value>Returns the purchase token.</value>
        string PurchaseToken { get; }
    }
}
