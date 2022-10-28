#nullable enable

using System.Collections.Generic;
using System.Threading;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreFinishTransactionService : IGooglePlayStoreFinishTransactionService
    {
        readonly HashSet<string> m_ProcessedPurchaseToken;
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreCallback? m_StoreCallback;
        int m_RetryCount = 0;
        const int k_MaxRetryAttempts = 5;

        internal GooglePlayStoreFinishTransactionService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_ProcessedPurchaseToken = new HashSet<string>();
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void SetStoreCallback(IStoreCallback? storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public void FinishTransaction(ProductDefinition? product, string? purchaseToken)
        {
            m_GooglePlayStoreService.FinishTransaction(product, purchaseToken,
                (billingResult, googlePurchase) => HandleFinishTransaction(product, billingResult, googlePurchase));
        }

        void HandleFinishTransaction(ProductDefinition? product, IGoogleBillingResult billingResult, IGooglePurchase purchase)
        {
            if (!m_ProcessedPurchaseToken.Contains(purchase.purchaseToken))
            {
                if (billingResult.responseCode == GoogleBillingResponseCode.Ok)
                {
                    m_RetryCount = 0;
                    m_ProcessedPurchaseToken.Add(purchase.purchaseToken);
                    CallPurchaseSucceededUpdateReceipt(purchase);
                }
                else if (m_RetryCount <= k_MaxRetryAttempts && IsResponseCodeInRecoverableState(billingResult))
                {
                    ++m_RetryCount;
                    FinishTransaction(product, purchase.purchaseToken);
                }
                else
                {
                    m_StoreCallback?.OnPurchaseFailed(
                        new PurchaseFailureDescription(
                            product?.storeSpecificId,
                            PurchaseFailureReason.Unknown,
                            billingResult.debugMessage + " {code: " + billingResult.responseCode + ", M: GPSFTS.HFT}"
                        )
                    );
                }
            }
        }

        void CallPurchaseSucceededUpdateReceipt(IGooglePurchase googlePurchase)
        {
            m_StoreCallback?.OnPurchaseSucceeded(
                googlePurchase.sku,
                googlePurchase.receipt,
                googlePurchase.purchaseToken
            );
        }

        static bool IsResponseCodeInRecoverableState(IGoogleBillingResult billingResult)
        {
            // DeveloperError is only a possible recoverable state because of this
            // https://github.com/android/play-billing-samples/issues/337
            // usually works like a charm next acknowledge
            return billingResult.responseCode == GoogleBillingResponseCode.ServiceUnavailable ||
                   billingResult.responseCode == GoogleBillingResponseCode.DeveloperError ||
                   billingResult.responseCode == GoogleBillingResponseCode.FatalError;
        }
    }
}
