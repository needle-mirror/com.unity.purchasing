#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
#if IAP_TX_VERIFIER_ENABLED
using UnityEngine.Purchasing.TransactionVerifier;
#endif
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AppleStoreExtendedPurchaseService : PurchaseService, IAppleStoreExtendedPurchaseService
    {
        readonly IAppReceiptUseCase m_AppReceiptUseCase;
        readonly IContinuePromotionalPurchasesUseCase m_ContinuePromotionalPurchasesUseCase;
        readonly IPresentCodeRedemptionSheetUseCase m_PresentCodeRedemptionSheetUseCase;
        readonly IRestoreTransactionsUseCase m_RestoreTransactionsUseCase;
        readonly ISetPromotionalPurchaseInterceptorCallbackUseCase m_SetPromotionalPurchaseInterceptorCallbackUseCase;
        readonly ISimulateAskToBuyUseCase m_SimulateAskToBuyUseCase;
        readonly IOnEntitlementRevokedUseCase m_OnEntitlementRevokedUseCase;
        readonly IRefreshAppReceiptUseCase m_RefreshAppReceiptUseCase;
        readonly IPurchaseEventEmitter m_PurchaseEvent;

        [Preserve]
        internal AppleStoreExtendedPurchaseService(
            IAppReceiptUseCase appReceiptUseCase,
            IContinuePromotionalPurchasesUseCase continuePromotionalPurchasesUseCase,
            IPresentCodeRedemptionSheetUseCase presentCodeRedemptionSheetUseCase,
            IRestoreTransactionsUseCase restoreTransactionsUseCase,
            ISetPromotionalPurchaseInterceptorCallbackUseCase setPromotionalPurchaseInterceptorCallbackUseCase,
            ISimulateAskToBuyUseCase simulateAskToBuyUseCase,
            IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IOnEntitlementRevokedUseCase onEntitlementRevokedUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient,
            IRefreshAppReceiptUseCase refreshAppReceiptUseCase,
            IPurchaseEventEmitter purchaseEvent
#if IAP_TX_VERIFIER_ENABLED
            , ITransactionVerifier transactionVerifier
#endif
            )
            : base(fetchPurchasesUseCase,
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
            m_AppReceiptUseCase = appReceiptUseCase;
            m_ContinuePromotionalPurchasesUseCase = continuePromotionalPurchasesUseCase;
            m_PresentCodeRedemptionSheetUseCase = presentCodeRedemptionSheetUseCase;
            m_RestoreTransactionsUseCase = restoreTransactionsUseCase;
            m_SetPromotionalPurchaseInterceptorCallbackUseCase = setPromotionalPurchaseInterceptorCallbackUseCase;
            m_SimulateAskToBuyUseCase = simulateAskToBuyUseCase;
            m_OnEntitlementRevokedUseCase = onEntitlementRevokedUseCase;

            m_OnEntitlementRevokedUseCase.OnEntitlementRevoked += OnEntitlementRevokedUseCaseOnOnEntitlementRevoked;
            m_RefreshAppReceiptUseCase = refreshAppReceiptUseCase;
            m_PurchaseEvent = purchaseEvent;
        }

        private protected override void SendPurchaseIntentStartEvent(ICart cart)
        {
            m_PurchaseEvent.SendPurchaseIntentStartEvent(cart);
        }

        private protected override void SendPurchasePaidEvent(PendingOrder order)
        {
            m_PurchaseEvent.SendPurchasePaidEvent(order, BuildApplePayload(order));
        }

        private protected override void SendPurchaseFailedEvent(FailedOrder order)
        {
            m_PurchaseEvent.SendPurchaseFailedEvent(order);
        }

        private protected override void SendPurchaseFulfilledEvent(ConfirmedOrder order)
        {
            var payload = BuildApplePayload(order);
            if (payload == null) return;
            m_PurchaseEvent.SendPurchaseFulfilledEvent(order, payload);
        }

        static ApplePurchaseFulfilledPayload? BuildApplePayload(Order order)
        {
            var apple = order.Info?.Apple;
            if (apple == null) return null;
            // Legacy StoreKit 1 surfaces an all-zeros Guid when no account
            // token was set; treat as "not set" per store_payload.proto.
            var token = apple.AppAccountToken;
            var appAccountToken = (token == null || token == Guid.Empty) ? null : token.Value.ToString();
            return new ApplePurchaseFulfilledPayload
            {
                JwsRepresentation = apple?.jwsRepresentation,
                OriginalTransactionId = apple?.OriginalTransactionID,
                Ownership = apple?.OwnershipType ?? OwnershipType.Undefined,
                AppReceipt = apple?.AppReceipt,
                AppAccountToken = appAccountToken
            };
        }

        void OnEntitlementRevokedUseCaseOnOnEntitlementRevoked(string productId)
        {
            var ordersToRemove = new List<Order>();

            foreach (var order in GetPurchases())
            {
                if (DoesOrderContainAnyRevokedProductIds(productId, order))
                {
                    ordersToRemove.Add(order);
                }
            }

            foreach (var order in ordersToRemove)
            {
                m_PurchaseCache.Remove(order);
            }
        }

        static bool DoesOrderContainAnyRevokedProductIds(string productId, Order order)
        {
            return order.CartOrdered.Items().Any(cartItem =>
                cartItem.Product.catalogListings.TryGetValue(cartItem.CatalogListingId, out var listing)
                && productId.Contains(listing.definition.storeSpecificId));
        }

        public string? appReceipt => m_AppReceiptUseCase.AppReceipt();

        public event Action<string>? OnEntitlementRevoked
        {
            add => m_OnEntitlementRevokedUseCase.OnEntitlementRevoked += value;
            remove => m_OnEntitlementRevokedUseCase.OnEntitlementRevoked -= value;
        }

        public void PresentCodeRedemptionSheet()
        {
            IsStoreConnected();
            m_PresentCodeRedemptionSheetUseCase.PresentCodeRedemptionSheet();
        }

        protected override void RestoreTransactionsInternal(Action<bool, string?>? callback)
        {
            m_RestoreTransactionsUseCase.RestoreTransactions(callback);
        }

        public event Action<Product>? OnPromotionalPurchaseIntercepted
        {
            add => m_SetPromotionalPurchaseInterceptorCallbackUseCase.OnPromotionalPurchaseIntercepted += value;
            remove => m_SetPromotionalPurchaseInterceptorCallbackUseCase.OnPromotionalPurchaseIntercepted -= value;
        }

        public void ContinuePromotionalPurchases()
        {
            m_ContinuePromotionalPurchasesUseCase.ContinuePromotionalPurchases();
        }

        public bool simulateAskToBuy
        {
            get => m_SimulateAskToBuyUseCase.SimulateAskToBuy();
            set => m_SimulateAskToBuyUseCase.SetSimulateAskToBuy(value);
        }

        // TODO: IAP-3929
        public void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
            m_RefreshAppReceiptUseCase.RefreshAppReceipt(successCallback, errorCallback);
        }

        // TODO: IAP-3929
        public void SetRefreshAppReceipt(bool refreshAppReceipt)
        {
            m_RefreshAppReceiptUseCase.SetRefreshAppReceipt(refreshAppReceipt);
        }
    }
}
