using System;
using System.Linq;

namespace UnityEngine.Purchasing
{
    class GooglePlayConfiguration : IGooglePlayConfiguration
    {
        public void SetServiceDisconnectAtInitializeListener(Action action)
        {
            UnityIAPServices.Store(GooglePlay.Name).OnStoreDisconnected += _ =>
            {
                action?.Invoke();
            };
        }

        public void SetQueryProductDetailsFailedListener(Action<int> action)
        {
            UnityIAPServices.Product(GooglePlay.Name).OnProductsFetchFailed += (_ =>
            {
                action.Invoke(0);
            });
        }

        public void SetDeferredPurchaseListener(Action<Product> action)
        {
            UnityIAPServices.Purchase(GooglePlay.Name).OnPurchaseDeferred += order =>
            {
                action.Invoke(order.CartOrdered.Items().FirstOrDefault()?.Product);
            };
        }

        public void SetDeferredProrationUpgradeDowngradeSubscriptionListener(Action<Product> action)
        {
            var googlePlayStoreExtendedPurchaseService = UnityIAPServices.Purchase(GooglePlay.Name).Google;
            if (googlePlayStoreExtendedPurchaseService != null)
            {
                googlePlayStoreExtendedPurchaseService.OnDeferredPaymentUntilRenewalDate += order =>
                {
                    action.Invoke(order.SubscriptionOrdered);
                };
            }
        }

        public void SetObfuscatedAccountId(string accountId)
        {
            UnityIAPServices.Store(GooglePlay.Name).Google?.SetObfuscatedAccountId(accountId);
        }

        public void SetObfuscatedProfileId(string profileId)
        {
            UnityIAPServices.Store(GooglePlay.Name).Google?.SetObfuscatedProfileId(profileId);
        }

        public void SetFetchPurchasesAtInitialize(bool enable)
        {
            UnityPurchasing.shouldFetchProductsAtInit = enable;
        }

        [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
        public void SetFetchPurchasesExcludeDeferred(bool exclude)
        {
        }
    }
}
