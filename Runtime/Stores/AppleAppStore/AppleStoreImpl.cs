#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using AOT;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Security;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// App Store implementation of <see cref="IStore"/>.
    /// </summary>
    class AppleStoreImpl : JSONStore, IAppleExtensions, IAppleConfiguration
    {
        Action<Product>? m_DeferredCallback;
        Action<List<Product>>? m_RevokedCallback;
        Action? m_RefreshReceiptError;
        Action<string>? m_RefreshReceiptSuccess;
        Action<bool>? m_RestoreCallback;
        Action? m_FetchStorePromotionOrderError;
        Action<List<Product>>? m_FetchStorePromotionOrderSuccess;
        Action<Product>? m_PromotionalPurchaseCallback;
        Action? m_FetchStorePromotionVisibilityError;
        Action<string, AppleStorePromotionVisibility>? m_FetchStorePromotionVisibilitySuccess;
        INativeAppleStore? m_Native;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        static IUtil? s_Util;
        static AppleStoreImpl? s_Instance;

        string? m_CachedAppReceipt;
        double? m_CachedAppReceiptModificationDate;


        string? m_ProductsJson;

        protected AppleStoreImpl(IUtil util, ITelemetryDiagnostics telemetryDiagnostics)
        {
            s_Util = util;
            s_Instance = this;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            m_ProductDescriptionsDeserializer = new AppleJsonProductDescriptionsDeserializer();
        }

        public void SetNativeStore(INativeAppleStore apple)
        {
            base.SetNativeStore(apple);
            m_Native = apple;
            apple.SetUnityPurchasingCallback(MessageCallback);
        }

        public string? appReceipt
        {
            get
            {
                var receiptModificationDate = appReceiptModificationDate;
                if (!m_CachedAppReceiptModificationDate.Equals(receiptModificationDate))
                {
                    m_CachedAppReceiptModificationDate = m_Native?.appReceiptModificationDate;
                    m_CachedAppReceipt = m_Native?.appReceipt;
                }

                return m_CachedAppReceipt;
            }
        }

        double? appReceiptModificationDate => m_Native?.appReceiptModificationDate;

        public bool canMakePayments => m_Native is { canMakePayments: true };

        public void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback)
        {
            m_PromotionalPurchaseCallback = callback;
        }

        public bool simulateAskToBuy
        {
            get => m_Native is { simulateAskToBuy: true };
            set
            {
                if (m_Native != null)
                {
                    m_Native.simulateAskToBuy = value;
                }
            }
        }

        public virtual void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionOrderError = errorCallback;
            m_FetchStorePromotionOrderSuccess = successCallback;

            m_Native?.FetchStorePromotionOrder();
        }

        public virtual void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionVisibilityError = errorCallback;
            m_FetchStorePromotionVisibilitySuccess = successCallback;

            m_Native?.FetchStorePromotionVisibility(product.definition.id);
        }

        public virtual void SetStorePromotionOrder(List<Product> products)
        {
            // Encode product list as a json doc containing an array of store-specific ids:
            // { "products": [ "ssid1", "ssid2" ] }
            var productIds = new List<string>();
            foreach (var p in products)
            {
                if (p != null && !string.IsNullOrEmpty(p.definition.storeSpecificId))
                {
                    productIds.Add(p.definition.storeSpecificId);
                }
            }
            var dict = new Dictionary<string, object> { { "products", productIds } };
            m_Native?.SetStorePromotionOrder(MiniJson.JsonEncode(dict));
        }

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visibility)
        {
            if (product == null)
            {
                var ex = new ArgumentNullException(nameof(product));
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.InvalidProductError, ex);
                throw ex;
            }
            m_Native?.SetStorePromotionVisibility(product.definition.storeSpecificId, visibility.ToString());
        }

        public string? GetTransactionReceiptForProduct(Product product)
        {
            return m_Native?.GetTransactionReceiptForProductId(product.definition.storeSpecificId);
        }

        public void SetApplicationUsername(string applicationUsername)
        {
            m_Native?.SetApplicationUsername(applicationUsername);
        }

        public override void OnProductsRetrieved(string json)
        {
            // base.OnProductsRetrieved (json); // Don't call this, because we want to enrich the products first

            // get product list
            var productDescriptions = m_ProductDescriptionsDeserializer.DeserializeProductDescriptions(json);
            List<ProductDescription>? finalProductDescriptions = null;

            m_ProductsJson = json;

            // parse app receipt
            var appleReceipt = GetAppleReceiptFromBase64String(appReceipt);
            if (HasInAppPurchaseReceipts(appleReceipt))
            {
                finalProductDescriptions = EnrichProductDescriptions(productDescriptions, appleReceipt!);
            }

            // Pass along the enriched product descriptions
            unity.OnProductsRetrieved(finalProductDescriptions ?? productDescriptions);

            // If there is a promotional purchase callback, tell the store to intercept those purchases.
            if (m_PromotionalPurchaseCallback != null)
            {
                m_Native?.InterceptPromotionalPurchases();
            }

            // Indicate we are ready to start receiving payments.
            m_Native?.AddTransactionObserver();
        }

        bool HasInAppPurchaseReceipts(AppleReceipt? appleReceipt)
        {
            return appleReceipt != null && appleReceipt.inAppPurchaseReceipts?.Length > 0;
        }

        List<ProductDescription> EnrichProductDescriptions(List<ProductDescription> productDescriptions, AppleReceipt appleReceipt)
        {
            // Enrich the product descriptions with parsed receipt data
            var finalProductDescriptions = new List<ProductDescription>();
            foreach (var productDescription in productDescriptions)
            {
                // JDRjr this Find may not be sufficient for subscriptions (or even multiple non-consumables?)
                var mostRecentReceipt = FindMostRecentReceipt(appleReceipt, productDescription.storeSpecificId);
                if (mostRecentReceipt == null)
                {
                    finalProductDescriptions.Add(productDescription);
                }
                else
                {
                    var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType), mostRecentReceipt.productType.ToString());
                    if (productType == AppleStoreProductType.AutoRenewingSubscription)
                    {
                        // if the product is auto-renewing subscription, filter the expired products
                        if (new SubscriptionInfo(mostRecentReceipt, null).isExpired() == Result.True)
                        {
                            finalProductDescriptions.Add(productDescription);
                        }
                        else
                        {
                            finalProductDescriptions.Add(
                                new ProductDescription(
                                    productDescription.storeSpecificId,
                                    productDescription.metadata,
                                    appReceipt,
                                    mostRecentReceipt.transactionID));
                        }
                    }
                    else if (productType == AppleStoreProductType.Consumable)
                    {
                        finalProductDescriptions.Add(productDescription);
                    }
                    else
                    {
                        finalProductDescriptions.Add(
                            new ProductDescription(
                                productDescription.storeSpecificId,
                                productDescription.metadata,
                                appReceipt,
                                mostRecentReceipt.transactionID));
                    }
                }
            }

            return finalProductDescriptions;
        }

        static AppleInAppPurchaseReceipt? FindMostRecentReceipt(AppleReceipt appleReceipt, string productId)
        {
            var foundReceipts = Array.FindAll(appleReceipt.inAppPurchaseReceipts, (r) => r.productID == productId);
            Array.Sort(foundReceipts, (b, a) => a.purchaseDate.CompareTo(b.purchaseDate));
            return FirstNonCancelledReceipt(foundReceipts);
        }

        static AppleInAppPurchaseReceipt? FirstNonCancelledReceipt(AppleInAppPurchaseReceipt[] foundReceipts)
        {
            foreach (var receipt in foundReceipts)
            {
                if (receipt.cancellationDate == DateTime.MinValue)
                {
                    return receipt;
                }
            }

            return null;
        }

        public virtual void RestoreTransactions(Action<bool> callback)
        {
            m_RestoreCallback = callback;
            m_Native?.RestoreTransactions();
        }

        public virtual void RefreshAppReceipt(Action<string> successCallback, Action errorCallback)
        {
            m_RefreshReceiptSuccess = successCallback;
            m_RefreshReceiptError = errorCallback;
            m_Native?.RefreshAppReceipt();
        }

        public void RegisterPurchaseDeferredListener(Action<Product> callback)
        {
            m_DeferredCallback = callback;
        }

        public void SetEntitlementsRevokedListener(Action<List<Product>> callback)
        {
            m_RevokedCallback = callback;
        }

        public virtual void ContinuePromotionalPurchases()
        {
            m_Native?.ContinuePromotionalPurchases();
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {
            return JSONSerializer.DeserializeSubscriptionDescriptions(m_ProductsJson);
        }

        public Dictionary<string, string> GetProductDetails()
        {
            return JSONSerializer.DeserializeProductDetails(m_ProductsJson);
        }

        public virtual void PresentCodeRedemptionSheet()
        {
            m_Native?.PresentCodeRedemptionSheet();
        }

        public void OnPurchaseDeferred(string productId)
        {
            if (null != m_DeferredCallback)
            {
                var product = unity.products.WithStoreSpecificID(productId);
                if (null != product)
                {
                    m_DeferredCallback(product);
                }
            }
        }

        public void OnPromotionalPurchaseAttempted(string productId)
        {
            if (null != m_PromotionalPurchaseCallback)
            {
                var product = unity.products.WithStoreSpecificID(productId);
                if (null != product)
                {
                    m_PromotionalPurchaseCallback(product);
                }
            }
        }

        public void OnTransactionsRestoredSuccess()
        {
            m_RestoreCallback?.Invoke(true);
        }

        public void OnTransactionsRestoredFail(string error)
        {
            m_RestoreCallback?.Invoke(false);
        }

        public void OnAppReceiptRetrieved(string receipt)
        {
            m_RefreshReceiptSuccess?.Invoke(receipt);
        }

        public void OnAppReceiptRefreshedFailed()
        {
            m_RefreshReceiptError?.Invoke();
        }

        void OnEntitlementsRevoked(string productIds)
        {
            var revokedProducts = new List<Product>();
            var appleReceipt = GetAppleReceiptFromBase64String(appReceipt);
            var productIdList = productIds.ArrayListFromJson();

            foreach (string productId in productIdList)
            {
                var product = unity.products.WithStoreSpecificID(productId);
                if (null == product)
                {
                    continue;
                }

                RevokeEntitlement(appleReceipt, productId, revokedProducts, product);
            }

            m_RevokedCallback?.Invoke(revokedProducts);
        }

        void RevokeEntitlement(AppleReceipt? appleReceipt, string productId, List<Product> revokedProducts, Product product)
        {
            if (HasInAppPurchaseReceipts(appleReceipt) && RestoreActiveEntitlement(appleReceipt!, productId))
            {
                return;
            }

            revokedProducts.Add(product);
            PurchasingManager.OnEntitlementRevoked(product);
        }

        bool RestoreActiveEntitlement(AppleReceipt appleReceipt, string productId)
        {
            var receipt = FindMostRecentReceipt(appleReceipt, productId);
            if (receipt != null)
            {
                unity.OnPurchaseSucceeded(productId, appReceipt, receipt.transactionID);
                return true;
            }

            return false;
        }

        public void OnFetchStorePromotionOrderSucceeded(string productIds)
        {
            if (null != m_FetchStorePromotionOrderSuccess)
            {
                var productIdList = productIds.ArrayListFromJson();
                var products = new List<Product>();

                foreach (var productId in productIdList)
                {
                    var product = unity.products.WithStoreSpecificID(productId as string);
                    products.Add(product);
                }

                m_FetchStorePromotionOrderSuccess(products);
            }
        }

        public void OnFetchStorePromotionOrderFailed()
        {
            m_FetchStorePromotionOrderError?.Invoke();
        }

        public void OnFetchStorePromotionVisibilitySucceeded(String result)
        {
            if (null != m_FetchStorePromotionVisibilitySuccess)
            {
                var resultDictionary = (
                    Json.Deserialize(result) as Dictionary<string, object>
                    )?.ToDictionary(k => k.Key, k => k.Value.ToString());

                var productId = resultDictionary?["productId"];
                var storePromotionVisibility = resultDictionary?["visibility"];
                Enum.TryParse(storePromotionVisibility, out AppleStorePromotionVisibility visibility);
                if (productId != null)
                {
                    m_FetchStorePromotionVisibilitySuccess(productId, visibility);
                }
            }
        }

        public void OnFetchStorePromotionVisibilityFailed()
        {
            m_FetchStorePromotionVisibilityError?.Invoke();
        }

        [MonoPInvokeCallback(typeof(UnityPurchasingCallback))]
        private static void MessageCallback(string subject, string payload, string receipt, string transactionId)
        {
            s_Util?.RunOnMainThread(() =>
            {
                s_Instance?.ProcessMessage(subject, payload, receipt, transactionId);
            });
        }

        void ProcessMessage(string subject, string payload, string receipt, string transactionId)
        {
            if (string.IsNullOrEmpty(receipt))
            {
                receipt = appReceipt ?? "";
            }

            switch (subject)
            {
                case "OnSetupFailed":
                    OnSetupFailed(payload);
                    break;
                case "OnProductsRetrieved":
                    OnProductsRetrieved(payload);
                    break;
                case "OnPurchaseSucceeded":
                    OnPurchaseSucceeded(payload, receipt, transactionId);
                    break;
                case "OnPurchaseFailed":
                    OnPurchaseFailed(payload);
                    break;
                case "onProductPurchaseDeferred":
                    OnPurchaseDeferred(payload);
                    break;
                case "onPromotionalPurchaseAttempted":
                    OnPromotionalPurchaseAttempted(payload);
                    break;
                case "onFetchStorePromotionOrderSucceeded":
                    OnFetchStorePromotionOrderSucceeded(payload);
                    break;
                case "onFetchStorePromotionOrderFailed":
                    OnFetchStorePromotionOrderFailed();
                    break;
                case "onFetchStorePromotionVisibilitySucceeded":
                    OnFetchStorePromotionVisibilitySucceeded(payload);
                    break;
                case "onFetchStorePromotionVisibilityFailed":
                    OnFetchStorePromotionVisibilityFailed();
                    break;
                case "onTransactionsRestoredSuccess":
                    OnTransactionsRestoredSuccess();
                    break;
                case "onTransactionsRestoredFail":
                    OnTransactionsRestoredFail(payload);
                    break;
                case "onAppReceiptRefreshed":
                    OnAppReceiptRetrieved(payload);
                    break;
                case "onAppReceiptRefreshFailed":
                    OnAppReceiptRefreshedFailed();
                    break;
                case "onEntitlementsRevoked":
                    OnEntitlementsRevoked(payload);
                    break;
            }
        }

        public override void OnPurchaseSucceeded(string id, string receipt, string transactionId)
        {
            if (IsValidPurchaseState(GetAppleReceiptFromBase64String(receipt), id))
            {
                base.OnPurchaseSucceeded(id, receipt, transactionId);
            }
            else
            {
                base.FinishTransaction(null, transactionId);
            }
        }

        AppleReceipt? GetAppleReceiptFromBase64String(string? receipt)
        {
            AppleReceipt? appleReceipt = null;
            if (!string.IsNullOrEmpty(receipt))
            {
                var parser = new AppleReceiptParser();
                try
                {
                    appleReceipt = parser.Parse(Convert.FromBase64String(receipt));
                }
                catch (Exception ex)
                {
                    m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.ParseReceiptTransactionError, ex);
                }
            }
            return appleReceipt;
        }

        bool IsValidPurchaseState(AppleReceipt? appleReceipt, string productId)
        {
            var isValid = true;
            if (HasInAppPurchaseReceipts(appleReceipt))
            {
                var mostRecentReceipt = FindMostRecentReceipt(appleReceipt!, productId);
                if (mostRecentReceipt != null)
                {
                    var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType), mostRecentReceipt.productType.ToString());
                    if (productType == AppleStoreProductType.AutoRenewingSubscription)
                    {
                        // if the product is auto-renewing subscription, check if this transaction is expired
                        if (new SubscriptionInfo(mostRecentReceipt, null).isExpired() == Result.True)
                        {
                            isValid = false;
                        }
                    }
                }
            }
            return isValid;
        }
    }
}
