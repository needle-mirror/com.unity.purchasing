#nullable enable

using System;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    class PurchasedProductInfo : IPurchasedProductInfo
    {
        public PurchasedProductInfo(string productId, string receipt, ProductType productType)
        {
            this.productId = productId;
            if (productType == ProductType.Subscription)
            {
                var subscriptionInfoHelper = new SubscriptionInfoHelper(receipt, productId, null);
                TryInitSubscriptionInfo(subscriptionInfoHelper);
            }
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
