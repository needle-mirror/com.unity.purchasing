using System;

namespace UnityEngine.Purchasing
{
    class SubscriptionChangeRequest
    {
        readonly Action<DeferredPaymentUntilRenewalDateOrder> m_OnPurchaseDeferredUntilRenewalDate;

        internal Product PreviousSubscription { get; }
        internal Product NewSubscription { get; }
        internal GooglePlayProrationMode ProrationMode { get; }

        internal SubscriptionChangeRequest(Product previousSubscription, Product newSubscription,
            GooglePlayProrationMode prorationMode)
        {
            PreviousSubscription = previousSubscription;
            NewSubscription = newSubscription;
            ProrationMode = prorationMode;
        }
    }
}
