#nullable enable
namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about a purchased product.
    /// </summary>
    public interface IPurchasedProductInfo
    {
        /// <summary>
        /// Identifier of the purchased product.
        /// </summary>
        string productId { get; }

        /// <summary>
        /// A container for a Product’s subscription-related information.
        /// Returns null for non-subscriptions.
        /// </summary>
        SubscriptionInfo? subscriptionInfo { get; }
    }
}
