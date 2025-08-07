#nullable enable

using System;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Security;
#if IAP_TX_VERIFIER_ENABLED
using UnityEngine.Purchasing.TransactionVerifier;
#endif
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreExtendedPurchaseService : PurchaseService, IGooglePlayStoreExtendedPurchaseService
    {
        readonly IGooglePlayChangeSubscriptionUseCase m_GooglePlayChangeSubscriptionUseCase;
        readonly IRestoreTransactionsUseCase m_RestoreTransactionsUseCase;

        [Preserve]
        internal GooglePlayStoreExtendedPurchaseService(
            IGooglePlayChangeSubscriptionUseCase googlePlayChangeSubscriptionUseCase,
            IRestoreTransactionsUseCase restoreTransactionsUseCase,
            IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient
#if IAP_TX_VERIFIER_ENABLED
            , ITransactionVerifier transactionVerifier
#endif
            )
            : base(
                fetchPurchasesUseCase,
                purchaseUseCase,
                confirmOrderUseCase,
                checkEntitlementUseCase,
                storeWrapper,
                analyticsClient
#if IAP_TX_VERIFIER_ENABLED
                , transactionVerifier
#endif
                )
        {
            m_GooglePlayChangeSubscriptionUseCase = googlePlayChangeSubscriptionUseCase;
            m_RestoreTransactionsUseCase = restoreTransactionsUseCase;
        }

        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct)
        {
            UpgradeDowngradeSubscription(oldProduct, newProduct, GooglePlayProrationMode.ImmediateWithoutProration);
        }

        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayProrationMode desiredProrationMode)
        {
            UpgradeDowngradeSubscription(oldProduct, newProduct, (GooglePlayReplacementMode)desiredProrationMode);
        }

        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayReplacementMode desiredReplacementMode)
        {
            try
            {
                var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
                var orders = purchaseService.GetPurchases();
                var order = orders.First(order => order.CartOrdered.Items().Contains(oldProduct)) ?? new ConfirmedOrder(new Cart(new CartItem(oldProduct)), new OrderInfo("", oldProduct.transactionID, ""));
                UpgradeDowngradeSubscription(order, newProduct, desiredReplacementMode);
            }
            catch (Exception e)
            {
                PurchaseFailed(new FailedOrder(new Cart(new CartItem(newProduct)), PurchaseFailureReason.Unknown, e.Message));
            }
        }

        public void UpgradeDowngradeSubscription(Order order, Product newProduct, GooglePlayReplacementMode desiredReplacementMode)
        {
            try
            {
                m_GooglePlayChangeSubscriptionUseCase.ChangeSubscription(order, newProduct, desiredReplacementMode);
            }
            catch (Exception e)
            {
                PurchaseFailed(new FailedOrder(new Cart(new CartItem(newProduct)), PurchaseFailureReason.Unknown, e.Message));
            }
        }

        public bool IsOrderDeferred(Order order)
        {
            var purchaseState = GetPurchaseState(order);
            return purchaseState == GooglePurchaseState.Refunded || purchaseState == GooglePurchaseState.Deferred;
        }

        public string? GetObfuscatedAccountId(Order order)
        {
            return order.Info.Google?.ObfuscatedAccountId;
        }

        public string? GetObfuscatedProfileId(Order order)
        {
            return order.Info.Google?.ObfuscatedProfileId;
        }

        public GooglePurchaseState? GetPurchaseState(Order order)
        {
            return order switch
            {
                DeferredOrder => GooglePurchaseState.Deferred,
                PendingOrder or ConfirmedOrder => GooglePurchaseState.Purchased,
                _ => null
            };
        }

        public event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate
        {
            add => m_GooglePlayChangeSubscriptionUseCase.OnDeferredPaymentUntilRenewalDate += value;
            remove => m_GooglePlayChangeSubscriptionUseCase.OnDeferredPaymentUntilRenewalDate -= value;
        }

        protected override void RestoreTransactionsInternal(Action<bool, string?>? callback)
        {
            m_RestoreTransactionsUseCase.RestoreTransactions(callback);
        }
    }
}
