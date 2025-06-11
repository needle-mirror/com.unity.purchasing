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
        /// <returns>Returns the obfuscated account id if it exists, otherwise null is returned.</returns>
        string? ObfuscatedAccountId { get; set; }

        /// <summary>
        /// The obfuscated profile id of the user who made the purchase.
        /// This requires using <typeparamref name="IGooglePlayConfiguration.SetObfuscatedProfileId"/> before the purchase is made.
        /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase#getAccountIdentifiers()">getAccountIdentifiers</a>
        /// </summary>
        /// <returns>Returns the obfuscated profile id if it exists, otherwise null is returned.</returns>
        string? ObfuscatedProfileId { get; set; }
    }
}
