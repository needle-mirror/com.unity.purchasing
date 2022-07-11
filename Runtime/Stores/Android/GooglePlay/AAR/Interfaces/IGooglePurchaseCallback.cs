using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseCallback
    {
        void SetStoreCallback(IStoreCallback storeCallback);
        void SetStoreConfiguration(IGooglePlayConfigurationInternal configuration);
        void OnPurchaseSuccessful(IGooglePurchase purchase, string receipt, string purchaseToken);
        void OnPurchaseFailed(PurchaseFailureDescription purchaseFailureDescription);
        void NotifyDeferredPurchase(IGooglePurchase purchase, string receipt, string purchaseToken);
        void NotifyDeferredProrationUpgradeDowngradeSubscription(string sku);
    }
}
