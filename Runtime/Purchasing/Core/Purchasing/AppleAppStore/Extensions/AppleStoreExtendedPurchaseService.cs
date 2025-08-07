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
            IRefreshAppReceiptUseCase refreshAppReceiptUseCase
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

            m_OnEntitlementRevokedUseCase.OnEntitlementRevoked += OnEntitlementOnEntitlementRevokedUseCaseOnOnEntitlementRevoked;
            m_RefreshAppReceiptUseCase = refreshAppReceiptUseCase;
        }

        void OnEntitlementOnEntitlementRevokedUseCaseOnOnEntitlementRevoked(string productId)
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
                m_Purchases.Remove(order);
            }
        }

        static bool DoesOrderContainAnyRevokedProductIds(string productId, Order order)
        {
            return order.CartOrdered.Items().Any(cartItem => productId.Contains(cartItem.Product.definition.storeSpecificId));
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
