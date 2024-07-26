#nullable enable

using System;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    class PurchasedProductInfo : IPurchasedProductInfo
    {
        public PurchasedProductInfo(string productId, string receipt)
        {
            this.productId = productId;
            var subscriptionInfoHelper = new SubscriptionInfoHelper(receipt, productId, null);
            TryInitSubscriptionInfo(subscriptionInfoHelper);
        }

        void TryInitSubscriptionInfo(SubscriptionInfoHelper subscriptionInfoHelper)
        {
            try
            {
                subscriptionInfo = subscriptionInfoHelper.GetSubscriptionInfo();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string productId { get; }
        public SubscriptionInfo? subscriptionInfo { get; private set; }
    }
}
