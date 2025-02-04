using System;

namespace UnityEngine.Purchasing
{
    class SubscriptionChangeRequest
    {
        readonly Action<DeferredPaymentUntilRenewalDateOrder> m_OnPurchaseDeferredUntilRenewalDate;

        internal Product PreviousSubscription { get; }
        internal Product NewSubscription { get; }
        internal GooglePlayReplacementMode ReplacementMode { get; }

        internal SubscriptionChangeRequest(Product previousSubscription, Product newSubscription,
            GooglePlayReplacementMode replacementMode)
        {
            PreviousSubscription = previousSubscription;
            NewSubscription = newSubscription;
            ReplacementMode = replacementMode;
        }
    }
}
