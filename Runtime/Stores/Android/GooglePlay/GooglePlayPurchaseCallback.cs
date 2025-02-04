#nullable enable

using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayPurchaseCallback : IGooglePurchaseCallback
    {
        IProductCache? m_ProductCache;
        IStorePurchaseCallback? m_PurchaseCallback;
        IStorePurchaseFetchCallback? m_PurchaseFetchCallback;
        IGooglePlayChangeSubscriptionCallback? m_ChangeSubscriptionCallback;
        readonly IGooglePurchaseConverter m_GooglePurchaseConverter;
        readonly IUtil m_Util;

        [Preserve]
        internal GooglePlayPurchaseCallback(IGooglePurchaseConverter googlePurchaseConverter, IUtil util)
        {
            m_GooglePurchaseConverter = googlePurchaseConverter;
            m_Util = util;
        }

        public void SetProductCache(IProductCache productCache)
        {
            m_ProductCache = productCache;
        }

        public void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback)
        {
            m_PurchaseCallback = purchaseCallback;
        }

        public void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchCallback)
        {
            m_PurchaseFetchCallback = fetchCallback;
        }

        public void SetChangeSubscriptionCallback(IGooglePlayChangeSubscriptionCallback changeSubscriptionCallback)
        {
            m_ChangeSubscriptionCallback = changeSubscriptionCallback;
        }

        public void OnPurchaseSuccessful(IGooglePurchase purchase)
        {
            var order = m_GooglePurchaseConverter.CreateOrderFromPurchase(purchase, m_ProductCache);
            OnOrderPurchaseSuccessful(order);
        }

        void OnOrderPurchaseSuccessful(Order order)
        {
            if (order is PendingOrder pendingOrder)
            {
                m_PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
            }
            else if (order is ConfirmedOrder confirmedOrder)
            {
                m_PurchaseFetchCallback?.OnAllPurchasesRetrieved(new List<ConfirmedOrder>() { confirmedOrder });
            }
        }

        public void OnPurchaseFailed(PurchaseFailureDescription purchaseFailureDescription)
        {
            m_PurchaseCallback?.OnPurchaseFailed(
                purchaseFailureDescription.ConvertToFailedOrder());
        }

        public void NotifyDeferredPurchase(IGooglePurchase purchase)
        {
            var order = (DeferredOrder)m_GooglePurchaseConverter.CreateOrderFromPurchase(purchase, m_ProductCache);
            m_PurchaseCallback?.OnPurchaseDeferred(order);
        }

        public void NotifyDeferredProrationUpgradeDowngradeSubscription(string sku)
        {
            m_ChangeSubscriptionCallback?.OnSubscriptionChangeDeferredUntilRenewal(sku);
        }

        public void NotifyUpgradeDowngradeSubscription(string sku)
        {
            m_ChangeSubscriptionCallback?.OnSubscriptionChange(sku);
        }
    }
}
