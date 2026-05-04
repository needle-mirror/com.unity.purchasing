#nullable enable

using System;
#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using Unity.XGamingRuntime;
#endif
using UnityEngine;

namespace UnityEngine.Purchasing
{
    class PurchasedProductInfo : IPurchasedProductInfo
    {
        public PurchasedProductInfo(string productId, string receipt, ProductType productType, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            this.productId = productId;
            if (productType == ProductType.Subscription)
            {
                var subscriptionInfoHelper = new SubscriptionInfoHelper(receipt, productId, null, subscriptionInfo);
                TryInitSubscriptionInfo(subscriptionInfoHelper);
            }
        }

#if IAP_GDK && MICROSOFT_GDK_SUPPORT
        public PurchasedProductInfo(string productId, ProductType productType, XStoreProduct xStoreProduct)
        {
            this.productId = productId;
            if (productType == ProductType.Subscription)
            {
                subscriptionInfo = new SubscriptionInfo(productId, xStoreProduct);
            }
        }
#endif

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
