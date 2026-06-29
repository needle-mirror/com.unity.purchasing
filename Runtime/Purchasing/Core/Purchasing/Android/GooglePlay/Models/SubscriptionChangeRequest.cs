using System;

namespace UnityEngine.Purchasing
{
    class SubscriptionChangeRequest
    {
        readonly Action<DeferredPaymentUntilRenewalDateOrder> m_OnPurchaseDeferredUntilRenewalDate;

        internal Order CurrentOrder { get; }
        internal CartItem NewSubscriptionItem { get; }
        internal Product NewSubscription => NewSubscriptionItem.Product;
        internal GooglePlayReplacementMode ReplacementMode { get; }

        internal SubscriptionChangeRequest(Order currentOrder, CartItem newSubscriptionItem,
            GooglePlayReplacementMode replacementMode)
        {
            CurrentOrder = currentOrder;
            NewSubscriptionItem = newSubscriptionItem;
            ReplacementMode = replacementMode;
        }
    }
}
