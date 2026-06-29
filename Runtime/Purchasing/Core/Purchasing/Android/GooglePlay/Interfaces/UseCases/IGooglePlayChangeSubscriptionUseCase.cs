#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for the use case of changing a subscription on the Google Play Store
    /// </summary>
    interface IGooglePlayChangeSubscriptionUseCase
    {
        /// <summary>
        /// Change a subscription asynchronously. DeferredUntilRenewalDate is signalled via the actions passed.
        /// The targeted catalog listing on the new subscription product is taken from the <see cref="CartItem.CatalogListingId"/>.
        /// </summary>
        /// <param name="currentOrder">The current order containing the subscription to be replaced.</param>
        /// <param name="newSubscriptionItem">The new subscription to be purchased, including the targeted catalog listing.</param>
        /// <param name="replacementMode">The replacement mode to be used.</param>
        void ChangeSubscription(Order currentOrder, CartItem newSubscriptionItem,
            GooglePlayReplacementMode replacementMode);

        event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate;
    }
}
