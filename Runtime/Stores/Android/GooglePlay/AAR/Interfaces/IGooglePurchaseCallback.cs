#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseCallback
    {
        void SetProductCache(IProductCache productCache);
        void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback);
        void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchCallback);
        void SetChangeSubscriptionCallback(IGooglePlayChangeSubscriptionCallback changeSubscriptionCallback);
        Task OnPurchaseSuccessful(IGooglePurchase purchase);
        void OnPurchaseFailed(PurchaseFailureDescription purchaseFailureDescription);
        Task NotifyDeferredPurchase(IGooglePurchase purchase);
        void NotifyDeferredProrationUpgradeDowngradeSubscription(string? sku);
        void NotifyUpgradeDowngradeSubscription(string? sku);
    }
}
