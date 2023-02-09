#nullable enable

using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class GooglePlayPurchaseCallback : IGooglePurchaseCallback
    {
        IStoreCallback? m_StoreCallback;
        IGooglePlayConfigurationInternal? m_GooglePlayConfigurationInternal;
        readonly IUtil m_Util;

        public GooglePlayPurchaseCallback(IUtil util)
        {
            m_Util = util;
        }

        public void SetStoreCallback(IStoreCallback storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public void SetStoreConfiguration(IGooglePlayConfigurationInternal configuration)
        {
            m_GooglePlayConfigurationInternal = configuration;
        }

        public void OnPurchaseSuccessful(IGooglePurchase purchase, string receipt, string purchaseToken)
        {
            m_StoreCallback?.OnPurchaseSucceeded(purchase.sku ?? string.Empty, receipt, purchaseToken);
        }

        public void OnPurchaseFailed(PurchaseFailureDescription purchaseFailureDescription)
        {
            m_StoreCallback?.OnPurchaseFailed(purchaseFailureDescription);
        }

        public void NotifyDeferredPurchase(IGooglePurchase purchase, string receipt, string purchaseToken)
        {
            m_Util.RunOnMainThread(() =>
                m_GooglePlayConfigurationInternal?.NotifyDeferredPurchase(m_StoreCallback, purchase, receipt,
                    purchaseToken));

        }

        public void NotifyDeferredProrationUpgradeDowngradeSubscription(string sku)
        {
            m_Util.RunOnMainThread(() =>
                m_GooglePlayConfigurationInternal?.NotifyDeferredProrationUpgradeDowngradeSubscription(m_StoreCallback,
                    sku));
        }
    }
}
