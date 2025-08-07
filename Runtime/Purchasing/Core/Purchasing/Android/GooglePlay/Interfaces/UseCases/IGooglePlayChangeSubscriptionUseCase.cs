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
        /// </summary>
        /// <param name="currentOrder">The current order containing the subscription to be replaced.</param>
        /// <param name="newSubscription">The subscription to be purchased.</param>
        /// <param name="replacementMode">The replacement mode to be used.</param>
        void ChangeSubscription(Order currentOrder, Product newSubscription,
            GooglePlayReplacementMode replacementMode);

        event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate;
    }
}
