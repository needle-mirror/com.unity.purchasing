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
        readonly IPurchaseEventEmitter m_PurchaseEvent;

        [Preserve]
        internal GooglePlayStoreExtendedPurchaseService(
            IGooglePlayChangeSubscriptionUseCase googlePlayChangeSubscriptionUseCase,
            IRestoreTransactionsUseCase restoreTransactionsUseCase,
            IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient,
            IPurchaseEventEmitter purchaseEvent
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
            m_PurchaseEvent = purchaseEvent;
            Application.focusChanged += OnFocusChanged;
        }

        private protected override void SendPurchaseIntentStartEvent(ICart cart)
        {
            m_PurchaseEvent.SendPurchaseIntentStartEvent(cart);
        }

        private protected override void SendPurchasePaidEvent(PendingOrder order)
        {
            m_PurchaseEvent.SendPurchasePaidEvent(order, BuildGooglePayload(order));
        }

        private protected override void SendPurchaseFailedEvent(FailedOrder order)
        {
            m_PurchaseEvent.SendPurchaseFailedEvent(order);
        }

        private protected override void SendPurchaseFulfilledEvent(ConfirmedOrder order)
        {
            m_PurchaseEvent.SendPurchaseFulfilledEvent(order, BuildGooglePayload(order));
        }

        static GooglePurchaseFulfilledPayload BuildGooglePayload(Order order)
        {
            string json = "";
            string signature = "";

            var receipt = order.Info.Receipt;

            if (!string.IsNullOrEmpty(receipt))
            {
                var unified = JsonUtility.FromJson<UnifiedReceipt>(receipt);

                if (unified != null && !string.IsNullOrEmpty(unified.Payload))
                {
                    var googleReceipt = JsonUtility.FromJson<GoogleReceiptPayload>(unified.Payload);

                    if (googleReceipt != null)
                    {
                        json = googleReceipt.json ?? "";
                        signature = googleReceipt.signature ?? "";
                    }
                }
            }

            return new GooglePurchaseFulfilledPayload
            {
                OriginalJson = json,
                Signature = signature
            };
        }

        // Local mirror of the GoogleReceiptEncoder output (json / signature /
        // skuDetails). The canonical GoogleReceipt model lives in
        // Unity.Purchasing.Stores which this asmdef can't reference; we only
        // need the two fields the proto requires, so a tiny local DTO is
        // simpler than restructuring asmdefs.
        [Serializable]
        class GoogleReceiptPayload
        {
            public string? json;
            public string? signature;
        }

        ~GooglePlayStoreExtendedPurchaseService()
        {
            Application.focusChanged -= OnFocusChanged;
        }

        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct)
        {
// Obsolete: GooglePlayProrationMode
#pragma warning disable 618, 612
            UpgradeDowngradeSubscription(oldProduct, newProduct, GooglePlayProrationMode.ImmediateWithoutProration);
#pragma warning restore 618, 612
        }

// Obsolete: GooglePlayProrationMode
#pragma warning disable 618, 612
        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayProrationMode desiredProrationMode)
        {
            UpgradeDowngradeSubscription(oldProduct, newProduct, (GooglePlayReplacementMode)desiredProrationMode);
        }
#pragma warning restore 618, 612

        public void UpgradeDowngradeSubscription(Product oldProduct, Product newProduct, GooglePlayReplacementMode desiredReplacementMode)
        {
            try
            {
                var purchaseService = UnityIAPServices.Purchase(GooglePlay.Name);
                var orders = purchaseService.GetPurchases();
// Obsolete: Product.transactionID
#pragma warning disable 618, 612
                var order = orders.First(order => order.CartOrdered.Items().Contains(oldProduct)) ?? new ConfirmedOrder(new Cart(new CartItem(oldProduct)), new OrderInfo("", oldProduct.transactionID, ""));
#pragma warning restore 618, 612
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
                m_GooglePlayChangeSubscriptionUseCase.ChangeSubscription(order, new CartItem(newProduct), desiredReplacementMode);
            }
            catch (Exception e)
            {
                PurchaseFailed(new FailedOrder(new Cart(new CartItem(newProduct)), PurchaseFailureReason.Unknown, e.Message));
            }
        }

        public void UpgradeDowngradeSubscription(Order currentOrder, string newCatalogListingId, GooglePlayReplacementMode desiredReplacementMode)
        {
            var newProduct = m_StoreWrapper.instance.ProductCache.FindByCatalogListingId(newCatalogListingId);
            if (newProduct == null)
            {
                var unknown = Product.CreateUnknownProduct(newCatalogListingId);
                PurchaseFailed(new FailedOrder(new Cart(new CartItem(unknown)), PurchaseFailureReason.ProductUnavailable,
                    $"No product found for catalog listing id '{newCatalogListingId}'."));
                return;
            }

            try
            {
                var cartItem = new CartItem(newProduct, newCatalogListingId);
                m_GooglePlayChangeSubscriptionUseCase.ChangeSubscription(currentOrder, cartItem, desiredReplacementMode);
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

        void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus && IsStoreConnected())
            {
                TryFetchPurchases(OnFetchSuccessProcessAndCache, null);
            }
        }
    }
}
