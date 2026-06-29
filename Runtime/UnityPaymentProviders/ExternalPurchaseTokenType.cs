#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Token types for Apple's ExternalPurchaseCustomLink API.
    /// Used when fetching a token to associate with an external purchase.
    /// </summary>
    public enum ExternalPurchaseTokenType
    {
        /// <summary>
        /// Use for acquiring new customers through external purchase links.
        /// </summary>
        Acquisition,

        /// <summary>
        /// Use for offering add-on services to existing customers.
        /// </summary>
        Services,

        /// <summary>
        /// Use for linking users to an external website for purchases (Japan MSCA compliance).
        /// </summary>
        LinkOut
    }
}
