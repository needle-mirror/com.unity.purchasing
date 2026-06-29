#nullable enable

namespace UnityEngine.Purchasing.PaymentProviders
{
    /// <summary>
    /// Identifies which app store a <see cref="PaymentProviderToken"/> targets.
    /// </summary>
    public enum PaymentProviderTokenStore
    {
        /// <summary>Apple App Store.</summary>
        Apple,
        /// <summary>Google Play Store.</summary>
        Google
    }

    /// <summary>
    /// One entry in the <c>externalTransactionTokens</c> array sent to the Payment Provider
    /// backend when creating an order. Apple supplies a typed token per region
    /// (Acquisition / Services for EU, LinkOut for Japan); Google supplies a single
    /// untyped BillingProgramReportingDetails token.
    /// </summary>
    public class PaymentProviderToken
    {
        /// <summary>
        /// The app store the token targets.
        /// </summary>
        public PaymentProviderTokenStore Store { get; }

        /// <summary>
        /// The raw token value returned by the platform.
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// Token classification. Apple-only; always <c>null</c> for Google.
        /// </summary>
        public ExternalPurchaseTokenType? Type { get; }

        public PaymentProviderToken(PaymentProviderTokenStore store, string token, ExternalPurchaseTokenType? type = null)
        {
            Store = store;
            Token = token;
            Type = type;
        }
    }
}