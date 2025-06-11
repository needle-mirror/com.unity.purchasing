using System;

namespace UnityEngine.Purchasing
{
    class SubscriptionChangeRequest
    {
        readonly Action<DeferredPaymentUntilRenewalDateOrder> m_OnPurchaseDeferredUntilRenewalDate;

        internal Order CurrentOrder { get; }
        internal Product NewSubscription { get; }
        internal GooglePlayReplacementMode ReplacementMode { get; }

        internal SubscriptionChangeRequest(Order currentOrder, Product newSubscription,
            GooglePlayReplacementMode replacementMode)
        {
            CurrentOrder = currentOrder;
            NewSubscription = newSubscription;
            ReplacementMode = replacementMode;
        }
    }
}
