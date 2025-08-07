using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreSetObfuscatedIdUseCase : IGooglePlayStoreSetObfuscatedIdUseCase
    {
        readonly IGoogleBillingClient m_BillingClient;

        [Preserve]
        internal GooglePlayStoreSetObfuscatedIdUseCase(IGoogleBillingClient billingClient)
        {
            m_BillingClient = billingClient;
        }

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase.
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="accountId">The obfuscated account id</param>
        public void SetObfuscatedAccountId(string accountId)
        {
            m_BillingClient.SetObfuscationAccountId(accountId);
        }

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="profileId">The obfuscated profile id</param>
        public void SetObfuscatedProfileId(string profileId)
        {
            m_BillingClient.SetObfuscationProfileId(profileId);
        }
    }
}
