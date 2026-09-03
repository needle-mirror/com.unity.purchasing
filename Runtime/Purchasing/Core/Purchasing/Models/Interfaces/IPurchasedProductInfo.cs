#nullable enable
namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about a purchased product.
    /// </summary>
    public interface IPurchasedProductInfo
    {
        /// <summary>
        /// Store-specific identifier of the purchased product.
        /// </summary>
        string productId { get; }

        /// <summary>
        /// A container for a Product’s subscription-related information.
        /// Returns null for non-subscriptions, and for products that were not fetched from the store.
        /// </summary>
        SubscriptionInfo? subscriptionInfo { get; }
    }
}
