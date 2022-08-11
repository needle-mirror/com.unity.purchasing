using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreExtensions : IGooglePlayStoreExtensions, IGooglePlayStoreExtensionsInternal
    {
        IGooglePlayStoreService m_GooglePlayStoreService;
        IGooglePlayStoreFinishTransactionService m_GooglePlayStoreFinishTransactionService;
        ILogger m_Logger;
        ITelemetryDiagnostics m_TelemetryDiagnostics;
        IStoreCallback m_StoreCallback;

        Action<Product> m_DeferredPurchaseAction;
        Action<Product> m_DeferredProrationUpgradeDowngradeSubscriptionAction;

        internal GooglePlayStoreExtensions(IGooglePlayStoreService googlePlayStoreService, IGooglePlayStoreFinishTransactionService googlePlayStoreFinishTransactionService, ILogger logger, ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_GooglePlayStoreFinishTransactionService = googlePlayStoreFinishTransactionService;
            m_Logger = logger;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku)
        {
            UpgradeDowngradeSubscription(oldSku, newSku, GooglePlayProrationMode.ImmediateWithoutProration);
        }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode)
        {
            UpgradeDowngradeSubscription(oldSku, newSku, (GooglePlayProrationMode)desiredProrationMode);
        }

        public virtual void UpgradeDowngradeSubscription(string oldSku, string newSku, GooglePlayProrationMode desiredProrationMode)
        {
            Product product = m_StoreCallback.FindProductById(newSku);
            Product oldProduct = m_StoreCallback.FindProductById(oldSku);
            if (product == null || product.definition.type != ProductType.Subscription ||
                oldProduct == null || oldProduct.definition.type != ProductType.Subscription)
            {
                m_StoreCallback?.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        newSku ?? "",
                        PurchaseFailureReason.ProductUnavailable,
                        "Please verify that the products are subscriptions and are not null."));
            }
            else if (string.IsNullOrEmpty(oldProduct.transactionID))
            {
                m_StoreCallback?.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        newSku ?? "",
                        PurchaseFailureReason.ProductUnavailable,
                        "Invalid transaction id for old product: " + oldProduct.definition.id));
            }
            else
            {
                m_GooglePlayStoreService.Purchase(product.definition, oldProduct, desiredProrationMode);
            }
        }

        public virtual void RestoreTransactions(Action<bool> callback)
        {
            if (callback == null)
            {
                m_Logger.LogIAPError("RestoreTransactions called with a null callback. Please provide a callback to avoid null pointer exceptions");
            }
            m_GooglePlayStoreService.FetchPurchases(_ => { callback?.Invoke(true); });
        }

        public void ConfirmSubscriptionPriceChange(string productId, Action<bool> callback)
        {
            Product product = m_StoreCallback.FindProductById(productId);
            if (product != null)
            {
                m_GooglePlayStoreService.ConfirmSubscriptionPriceChange(product.definition, result =>
                {
                    callback(result.responseCode == GoogleBillingResponseCode.Ok);
                });
            }
        }

        public void SetStoreCallback(IStoreCallback storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public bool IsPurchasedProductDeferred(Product product)
        {
            try
            {
                var unifiedReceipt = MiniJson.JsonDecode(product.receipt) as Dictionary<string, object>;
                var payloadStr = unifiedReceipt["Payload"] as string;

                var payload = MiniJson.JsonDecode(payloadStr) as Dictionary<string, object>;
                var jsonStr = payload["json"] as string;

                var jsonDic = MiniJson.JsonDecode(jsonStr) as Dictionary<string, object>;
                var purchaseState = (long)jsonDic["purchaseState"];

                //PurchaseState codes: https://developer.android.com/reference/com/android/billingclient/api/Purchase.PurchaseState#pending
                return purchaseState == 2 || purchaseState == 4;
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.ParseReceiptTransactionError, ex);
                Debug.LogWarning("Cannot parse Google receipt for transaction " + product.transactionID);
                return false;
            }
        }
    }
}
