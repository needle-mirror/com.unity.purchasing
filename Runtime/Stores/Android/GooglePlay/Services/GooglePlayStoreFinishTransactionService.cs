#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreFinishTransactionService : IGooglePlayStoreFinishTransactionService
    {
        readonly HashSet<string?> m_ProcessedPurchaseToken;
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IProductCache? m_ProductCache;
        IStorePurchaseConfirmCallback? m_ConfirmCallback;
        int m_RetryCount = 0;
        const int k_MaxRetryAttempts = 5;

        [Preserve]
        internal GooglePlayStoreFinishTransactionService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_ProcessedPurchaseToken = new HashSet<string?>();
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void SetProductCache(IProductCache? productCache)
        {
            m_ProductCache = productCache;
        }

        public void SetConfirmCallback(IStorePurchaseConfirmCallback confirmCallback)
        {
            m_ConfirmCallback = confirmCallback;
        }

        public async void FinishTransaction(ProductDefinition? product, string purchaseToken)
        {
            try
            {
                await m_GooglePlayStoreService.FinishTransaction(product, purchaseToken,
                    (billingResult, googlePurchase) => HandleFinishTransaction(product, billingResult, googlePurchase));
            }
            catch (Exception e)
            {
                SendTransactionFailedCallback(
                    new PurchaseFailureDescription(
                        m_ProductCache?.FindOrDefault(product?.storeSpecificId) ??
                        Product.CreateUnknownProduct(product?.storeSpecificId),
                        PurchaseFailureReason.Unknown,
                        e.Message
                    ), purchaseToken
                );
            }

        }

        void HandleFinishTransaction(ProductDefinition? product, IGoogleBillingResult billingResult, IGooglePurchase purchase)
        {
            // Only process if token has not been seen before
            if (m_ProcessedPurchaseToken.Contains(purchase.purchaseToken))
            {
                return;
            }

            // Success path: Transaction completed successfully by Google Play
            if (billingResult.responseCode == GoogleBillingResponseCode.Ok)
            {
                m_RetryCount = 0;
                m_ProcessedPurchaseToken.Add(purchase.purchaseToken);
                CallPurchaseSucceededUpdateReceipt(purchase);
                return;
            }

            // Retry path: Recoverable error occurred, attempt retry if within limits
            if (m_RetryCount <= k_MaxRetryAttempts && IsResponseCodeInRecoverableState(billingResult))
            {
                ++m_RetryCount;
                FinishTransaction(product, purchase.purchaseToken);
                return;
            }

            // Fallback error path: All other cases are treated as failures
            // This includes: non-recoverable errors, exhausted retry attempts, or unexpected response codes
            SendTransactionFailedCallback(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(product?.storeSpecificId) ??
                    Product.CreateUnknownProduct(product?.storeSpecificId),
                    PurchaseFailureReason.Unknown,
                    billingResult.debugMessage + " {code: " + billingResult.responseCode + ", M: GPSFTS.HFT}"
                ), purchase.purchaseToken
            );
        }

        void SendTransactionFailedCallback(PurchaseFailureDescription purchaseFailureDescription, string? purchaseToken)
        {
            m_ConfirmCallback?.OnConfirmOrderFailed(purchaseFailureDescription.ConvertToFailedOrder(purchaseToken));
        }

        void CallPurchaseSucceededUpdateReceipt(IGooglePurchase googlePurchase)
        {
            m_ConfirmCallback?.OnConfirmOrderSucceeded(googlePurchase.purchaseToken);
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
