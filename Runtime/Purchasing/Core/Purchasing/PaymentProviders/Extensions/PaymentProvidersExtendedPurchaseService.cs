#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviders;
using UnityEngine.Scripting;
#if IAP_TX_VERIFIER_ENABLED
using UnityEngine.Purchasing.TransactionVerifier;
using UnityEngine.Purchasing.TransactionVerifier.Http;
#endif

namespace UnityEngine.Purchasing
{
    internal class PaymentProvidersExtendedPurchaseService : PurchaseService, IPaymentProvidersExtendedPurchaseService
    {
        IPaymentProviderCallbacks m_PaymentProviderCallbacks;
        readonly IPurchaseEventEmitter m_PurchaseEvent;

        [Preserve]
        internal PaymentProvidersExtendedPurchaseService(
            IFetchPurchasesUseCase fetchPurchasesUseCase,
            IPurchaseUseCase purchaseUseCase,
            IConfirmOrderUseCase confirmOrderUseCase,
            ICheckEntitlementUseCase checkEntitlementUseCase,
            IStoreWrapper storeWrapper,
            IAnalyticsClient analyticsClient,
            IPaymentProviderCallbacks paymentProviderCallbacks,
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
            m_PaymentProviderCallbacks = paymentProviderCallbacks;
            m_PurchaseEvent = purchaseEvent;
        }

        // Exposed for the sibling store-extended service so the Payment Option
        // Provider modal can fire telemetry through the same emitter that the
        // PaymentProvider purchase flow uses. Internal — not part of the
        // public IPaymentProvidersExtendedPurchaseService surface.
        internal IPurchaseEventEmitter PurchaseEventEmitter => m_PurchaseEvent;

        public void SetPaymentProviderOverride(string? paymentProviderOverride)
        {
            m_PaymentProviderCallbacks.SetPaymentProviderOverride(paymentProviderOverride);
        }

        public void SetComplianceCheck(Func<PaymentProviderComplianceContext, Task<bool>>? complianceCheck)
        {
            m_PaymentProviderCallbacks.SetComplianceCheck(complianceCheck);
        }

        public Task<string?> GenerateURL(string? catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens = null)
        {
            return m_PaymentProviderCallbacks.GenerateURL(catalogListingId, externalTokens);
        }

        public Task RedirectToWebshop(string? catalogListingId = null, IReadOnlyList<PaymentProviderToken>? externalTokens = null)
        {
            return m_PaymentProviderCallbacks.RedirectToWebshop(catalogListingId, externalTokens);
        }

        public void Purchase(ICart cart, string paymentProviderName)
        {
            m_PaymentProviderCallbacks.Purchase(cart, paymentProviderName);
        }

        public void PurchaseProduct(string catalogListingId, string paymentProviderName)
        {
            m_PaymentProviderCallbacks.PurchaseProduct(catalogListingId, paymentProviderName);
        }

        protected override void RestoreTransactionsInternal(Action<bool, string?>? callback)
        {
            callback?.Invoke(true, null);
        }

        // Payment-provider purchases hand the player off to an external
        // surface (webshop / Stripe / Coda / etc.) — paid/failed/fulfilled
        // events are handled by the backend. Only the intent-start
        // event is emitted from the SDK.
        private protected override void SendPurchaseIntentStartEvent(ICart cart)
        {
            m_PurchaseEvent.SendPurchaseIntentStartEvent(cart);
        }
    }
}
