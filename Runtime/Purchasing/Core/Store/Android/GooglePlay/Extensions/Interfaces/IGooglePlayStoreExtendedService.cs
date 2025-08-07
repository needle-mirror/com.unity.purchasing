using System;


namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Google Play Store service extension.
    /// </summary>
    public interface IGooglePlayStoreExtendedService : IStoreServiceExtension
    {
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
        /// End the connection with the Google Play Store.
        /// </summary>
        void EndConnection();
    }
}
