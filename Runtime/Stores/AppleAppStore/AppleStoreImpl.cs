#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AOT;
using Uniject;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Security;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// App Store implementation of <see cref="IStore"/>.
    /// </summary>
    class AppleStoreImpl : JsonStore, IAppleStoreCallbacks, IAppleAppReceiptViewer
    {
        Action<string>? m_RefreshReceiptError;
        Action<string>? m_RefreshReceiptSuccess;
        Action<bool>? m_ObsoleteRestoreCallback;
        Action<bool, string?>? m_RestoreCallback;
        Action? m_FetchStorePromotionOrderError;
        Action<List<Product>>? m_FetchStorePromotionOrderSuccess;
        Action<Product>? m_PromotionalPurchaseCallback;
        Action? m_FetchStorePromotionVisibilityError;
        Action<string, AppleStorePromotionVisibility>? m_FetchStorePromotionVisibilitySuccess;
        INativeAppleStore? m_Native;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;
        readonly IAppleRetrieveProductsService m_RetrieveProductsService;
        readonly ITransactionLog m_TransactionLog;
        readonly IAppleReceiptConverter m_ReceiptConverter;

        static IUtil? s_Util;
        static AppleStoreImpl? s_Instance;

        readonly ConcurrentDictionary<string, PendingOrder> m_PendingOrders = new ConcurrentDictionary<string, PendingOrder>();
        readonly ConcurrentDictionary<string, ConfirmedOrder> m_ConfirmedOrders = new ConcurrentDictionary<string, ConfirmedOrder>();
        string? m_CachedAppReceipt;
        double? m_CachedAppReceiptModificationDate;

        protected AppleStoreImpl(ICartValidator cartValidator, IAppleRetrieveProductsService retrieveProductsService,
            IAppleReceiptConverter receiptConverter,
            ITransactionLog transactionLog,
            IUtil util,
            ILogger logger,
            ITelemetryDiagnostics telemetryDiagnostics)
            : base(cartValidator, logger, DefaultStoreHelper.GetDefaultStoreName())
        {
            s_Util = util;
            s_Instance = this;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            m_ReceiptConverter = receiptConverter;
            m_RetrieveProductsService = retrieveProductsService;
            m_TransactionLog = transactionLog;
        }

        public void SetNativeStore(INativeAppleStore apple)
        {
            base.SetNativeStore(apple);
            m_Native = apple;
            m_RetrieveProductsService.SetNativeStore(apple);
            apple.SetUnityPurchasingCallback(MessageCallback);
        }

        public INativeAppleStore? GetNativeStore()
        {
            return m_Native;
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

        public bool canMakePayments => m_Native?.canMakePayments ?? false;

        public override void Connect()
        {
            m_Native?.Connect();
            ConnectCallback?.OnStoreConnectionSucceeded();
        }

        protected override void FinishTransaction(ProductDefinition? productDefinition, string transactionId)
        {
            ConfirmPendingOrder(transactionId);
            m_TransactionLog.Record(transactionId);
            base.FinishTransaction(productDefinition, transactionId);
        }

        void ConfirmPendingOrder(string transactionId)
        {
            if (!m_PendingOrders.TryRemove(transactionId, out var pendingOrder))
            {
                return;
            }

            m_ConfirmedOrders.TryAdd(transactionId, new ConfirmedOrder(pendingOrder.CartOrdered, pendingOrder.Info));
        }

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

        public override async void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            try
            {
                var productDescriptions = await m_RetrieveProductsService.RetrieveProducts(products);
                ProductCache.Add(productDescriptions);

                // Pass along the enriched product descriptions
                ProductsCallback?.OnProductsRetrieved(productDescriptions);

                // If there is a promotional purchase callback, tell the store to intercept those purchases.
                if (m_PromotionalPurchaseCallback != null)
                {
                    m_Native?.InterceptPromotionalPurchases();
                }
            }
            catch (RetrieveProductsException exception)
            {
                ProductsCallback?.OnProductsRetrieveFailed(exception.FailureDescription);
            }
        }

        void CreateFetchedOrders(string storeSpecificId, string transactionId, string originalTransactionId)
        {
            // TODO: IAP-3116 - Obsolete or to be improved
            if (transactionId == null)
            {
                return;
            }

            if (m_TransactionLog.HasRecordOf(transactionId))
            {
                AddConfirmedOrder(storeSpecificId, transactionId, originalTransactionId, false);
            }
            else
            {
                AddPendingOrder(storeSpecificId, transactionId, originalTransactionId, false);
            }
        }

        public override void FetchPurchases()
        {
            var appleReceipt = m_ReceiptConverter.ConvertFromBase64String(appReceipt);
            if (appleReceipt == null)
                return;

            if (appleReceipt.HasInAppPurchaseReceipts())
            {
                foreach (var product in ProductCache.productsById.Values)
                {
                    var mostRecentReceipt = appleReceipt.FindMostRecentReceiptForProduct(product.definition.storeSpecificId);
                    if (mostRecentReceipt != null)
                    {
                        CreateFetchedOrders(product.definition.storeSpecificId, mostRecentReceipt.transactionID, mostRecentReceipt.originalTransactionIdentifier);
                    }
                }
            }
            var fetchedOrders = m_PendingOrders.Values.Concat<Order>(m_ConfirmedOrders.Values).ToList();
            OnPurchasesFetched(fetchedOrders);
        }

        public virtual void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionOrderError = errorCallback;
            m_FetchStorePromotionOrderSuccess = successCallback;

            m_Native?.FetchStorePromotionOrder();
        }

        public virtual void FetchStorePromotionVisibility(Product product,
            Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionVisibilityError = errorCallback;
            m_FetchStorePromotionVisibilitySuccess = successCallback;

            m_Native?.FetchStorePromotionVisibility(product.definition.id);
        }

        public void SetFetchStorePromotionOrderCallbacks(Action<List<Product>> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionOrderError = errorCallback;
            m_FetchStorePromotionOrderSuccess = successCallback;
        }

        public void SetFetchStorePromotionVisibilityCallbacks(Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            m_FetchStorePromotionVisibilityError = errorCallback;
            m_FetchStorePromotionVisibilitySuccess = successCallback;
        }

        public void SetRefreshAppReceiptCallbacks(Action<string> successCallback, Action<string> errorCallback)
        {
            m_RefreshReceiptSuccess = successCallback;
            m_RefreshReceiptError = errorCallback;
        }

        public void SetRestoreTransactionsCallback(Action<bool, string?> successCallback)
        {
            m_RestoreCallback = successCallback;
        }

        public void ClearTransactionLog()
        {
            m_TransactionLog.Clear();
        }

        public string? GetAppReceipt()
        {
            return appReceipt;
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

        public string GetTransactionReceiptForProduct(Product product)
        {
            return m_Native?.GetTransactionReceiptForProductId(product.definition.storeSpecificId) ?? string.Empty;
        }

        public void SetApplicationUsername(string applicationUsername)
        {
            m_Native?.SetApplicationUsername(applicationUsername);
        }

        [Obsolete("Now done by AddProductDescriptionsToOrders")]
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
                        if (new SubscriptionInfo(mostRecentReceipt, null).IsExpired() == Result.True)
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

                            UpdateAppleProductFields(productDescription.storeSpecificId,
                                mostRecentReceipt.originalTransactionIdentifier,
                                true);
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

                        UpdateAppleProductFields(productDescription.storeSpecificId,
                            mostRecentReceipt.originalTransactionIdentifier,
                            true);
                    }
                }
            }

            return finalProductDescriptions;
        }

        static AppleInAppPurchaseReceipt? FindMostRecentReceipt(AppleReceipt? appleReceipt, string productId)
        {
            if (appleReceipt == null)
            {
                return null;
            }

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

        public virtual void RestoreTransactions(Action<bool, string?>? callback)
        {
            m_RestoreCallback = callback;
            m_Native?.RestoreTransactions();
        }

        public virtual void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
            m_RefreshReceiptSuccess = successCallback;
            m_RefreshReceiptError = errorCallback;
            m_Native?.RefreshAppReceipt();
        }

        public virtual void ContinuePromotionalPurchases()
        {
            m_Native?.ContinuePromotionalPurchases();
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {
            return JSONSerializer.DeserializeSubscriptionDescriptions(m_RetrieveProductsService
                .LastRequestProductsJson);
        }

        public Dictionary<string, string> GetProductDetails()
        {
            return JSONSerializer.DeserializeProductDetails(m_RetrieveProductsService.LastRequestProductsJson);
        }

        public virtual void PresentCodeRedemptionSheet()
        {
            m_Native?.PresentCodeRedemptionSheet();
        }

        void OnPurchaseDeferred(string id, string receipt, string transactionId, string originalTransactionId)
        {
            if (PurchaseCallback != null)
            {
                UpdateAppleProductFields(id, originalTransactionId, false);
                var deferredOrder = GenerateDeferredOrder(id, receipt, transactionId);
                PurchaseCallback.OnPurchaseDeferred(deferredOrder);
            }
        }

        public void OnPromotionalPurchaseAttempted(string productId)
        {
            if (null != m_PromotionalPurchaseCallback)
            {
                var product = ProductCache.Find(productId);
                if (null != product)
                {
                    m_PromotionalPurchaseCallback(product);
                }
            }
        }

        void OnPurchasesFetched(List<Order> orders)
        {
            PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
        }

        public void OnTransactionsRestoredSuccess()
        {
            m_RestoreCallback?.Invoke(true, null);
        }

        public void OnTransactionsRestoredFail(string error)
        {
            m_RestoreCallback?.Invoke(false, error);
        }

        public void OnAppReceiptRetrieved(string receipt)
        {
            m_RefreshReceiptSuccess?.Invoke(receipt);
        }

        public void OnAppReceiptRefreshedFailed(string error)
        {
            m_RefreshReceiptError?.Invoke(error);
        }

        void OnEntitlementsRevoked(string productIds)
        {
            var appleReceipt = m_ReceiptConverter.ConvertFromBase64String(appReceipt);
            if (appleReceipt == null)
            {
                return;
            }

            var productIdList = productIds.ArrayListFromJson().Cast<string>().ToList();

            RevokeEntitlement(productIdList);
        }

        void RevokeEntitlement(List<string> productIds)
        {
            EntitlementRevokedCallback?.OnEntitlementsRevoked(productIds);
        }

        public void OnFetchStorePromotionOrderSucceeded(string productIds)
        {
            if (null != m_FetchStorePromotionOrderSuccess)
            {
                var productIdList = productIds.ArrayListFromJson();
                var products = new List<Product>();

                foreach (var productId in productIdList)
                {
                    var product = ProductCache.FindOrDefault(productId as string);
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

                var productId = resultDictionary?["productId"] ?? String.Empty;
                var storePromotionVisibility = resultDictionary?["visibility"];
                Enum.TryParse(storePromotionVisibility, out AppleStorePromotionVisibility visibility);
                m_FetchStorePromotionVisibilitySuccess(productId, visibility);
            }
        }

        public void OnFetchStorePromotionVisibilityFailed()
        {
            m_FetchStorePromotionVisibilityError?.Invoke();
        }

        [MonoPInvokeCallback(typeof(UnityPurchasingCallback))]
        static void MessageCallback(string subject, string payload, string receipt, string transactionId, string originalTransactionId, bool isRestored)
        {
            s_Util?.RunOnMainThread(() =>
            {
                s_Instance?.ProcessMessage(subject, payload, receipt, transactionId, originalTransactionId, isRestored);
            });
        }

        void ProcessMessage(string subject, string payload, string receipt, string transactionId, string originalTransactionId, bool isRestored)
        {
            if (string.IsNullOrEmpty(receipt))
            {
                receipt = appReceipt ?? "";
            }

            switch (subject)
            {
                case "OnProductsRetrieved":
                    m_RetrieveProductsService.OnProductsRetrieved(payload);
                    break;
                case "OnProductsRetrieveFailed":
                    m_RetrieveProductsService.OnProductDetailsRetrieveFailed(payload);
                    break;
                case "OnPurchaseSucceeded":
                    OnPurchaseSucceeded(payload, receipt, transactionId, originalTransactionId, isRestored);
                    break;
                case "OnPurchaseFailed":
                    OnPurchaseFailed(payload);
                    break;
                case "onProductPurchaseDeferred":
                    OnPurchaseDeferred(payload, receipt, transactionId, originalTransactionId);
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
                    OnAppReceiptRefreshedFailed(payload);
                    break;
                case "onEntitlementsRevoked":
                    OnEntitlementsRevoked(payload);
                    break;
            }
        }

        public override void CheckEntitlement(ProductDefinition productDefinition)
        {
            if (productDefinition.type == ProductType.Unknown)
            {
                EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.Unknown);
                return;
            }

            if (CheckEntitlementConfirmedOrders(productDefinition) ||
                CheckEntitlementPendingOrders(productDefinition))
            {
                return;
            }

            EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.NotEntitled);
        }

        bool CheckEntitlementConfirmedOrders(ProductDefinition productDefinition)
        {
            foreach (var order in m_ConfirmedOrders)
            {
                if (order.Value.CartOrdered.Items().First()?.Product.definition.storeSpecificId == productDefinition.storeSpecificId)
                {
                    if (CheckEntitlementConfirmedOrdersByProduct(productDefinition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool CheckEntitlementConfirmedOrdersByProduct(ProductDefinition productDefinition)
        {
            switch (productDefinition.type)
            {
                case ProductType.NonConsumable:
                    EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.FullyEntitled);
                    return true;
                case ProductType.Subscription:
                {
                    var appleReceipt = m_ReceiptConverter.ConvertFromBase64String(appReceipt);

                    var mostRecentReceipt =
                        appleReceipt?.FindMostRecentReceiptForProduct(productDefinition.storeSpecificId);

                    if (new SubscriptionInfo(mostRecentReceipt, null).IsExpired() == Result.False)
                    {
                        EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.FullyEntitled);
                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        bool CheckEntitlementPendingOrders(ProductDefinition productDefinition)
        {
            foreach (var order in m_PendingOrders)
            {
                if (order.Value.CartOrdered.Items().First()?.Product.definition.storeSpecificId == productDefinition.storeSpecificId)
                {
                    switch (productDefinition.type)
                    {
                        case ProductType.Consumable:
                            EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.EntitledUntilConsumed);
                            return true;
                        case ProductType.NonConsumable:
                        case ProductType.Subscription:
                            EntitlementCallback?.OnCheckEntitlementSucceeded(productDefinition, EntitlementStatus.EntitledButNotFinished);
                            return true;
                    }
                }
            }

            return false;
        }

        public void OnPurchaseSucceeded(string id, string receipt, string transactionId, string originalTransactionId, bool isRestored)
        {
            var appleReceipt = GetAppleReceiptFromBase64String(receipt);
            var mostRecentReceipt = FindMostRecentReceipt(appleReceipt!, id);
            if (IsValidPurchaseState(mostRecentReceipt, id))
            {
                isRestored = isRestored || IsRestored(id, mostRecentReceipt, transactionId, originalTransactionId);
                ProcessValidPurchase(id, transactionId, originalTransactionId, isRestored);
            }
            else
            {
                base.FinishTransaction(null, transactionId);
            }
        }

        void ProcessValidPurchase(string id, string transactionId, string originalTransactionId, bool isRestored)
        {
            if (!m_TransactionLog.HasRecordOf(transactionId))
            {
                ProcessNewPurchase(id, transactionId, originalTransactionId, isRestored);
            }
            else
            {
                ProcessLoggedPurchase(id, transactionId, originalTransactionId, isRestored);
            }
        }

        void ProcessNewPurchase(string id, string transactionId, string originalTransactionId, bool isRestored)
        {
            var pendingOrder = AddPendingOrder(id, transactionId, originalTransactionId, isRestored);
            UpdateAppleProductFields(transactionId, originalTransactionId, isRestored);
            PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
        }

        PendingOrder AddPendingOrder(string id, string transactionId, string originalTransactionId, bool isRestored)
        {
            var order = GenerateApplePendingOrder(id, transactionId, originalTransactionId, isRestored);
            m_PendingOrders.TryAdd(transactionId, order);
            return order;
        }

        void ProcessLoggedPurchase(string id, string transactionId, string originalTransactionId, bool isRestored)
        {
            m_PendingOrders.TryRemove(transactionId, out _);
            var confirmedOrder = AddConfirmedOrder(id, transactionId, originalTransactionId, isRestored);
            UpdateAppleProductFields(transactionId, originalTransactionId, isRestored);
            EnsureConfirmedOrderIsFinished(confirmedOrder);
        }

        ConfirmedOrder AddConfirmedOrder(string id, string transactionId, string originalTransactionId, bool isRestored)
        {
            var order = GenerateAppleConfirmedOrder(id, transactionId, originalTransactionId, isRestored);
            m_ConfirmedOrders.TryAdd(transactionId, order);
            return order;
        }

        PendingOrder GenerateApplePendingOrder(string id, string transactionID, string originalTransactionId, bool isRestored)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new PendingOrder(cart, new AppleOrderInfo(transactionID, m_storeName, this, originalTransactionId, isRestored));
        }

        ConfirmedOrder GenerateAppleConfirmedOrder(string id, string transactionID, string originalTransactionId, bool isRestored)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new ConfirmedOrder(cart, new AppleOrderInfo(transactionID, m_storeName, this, originalTransactionId, isRestored));
        }

        void EnsureConfirmedOrderIsFinished(ConfirmedOrder confirmedOrder)
        {
            var productDefinition = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition;
            base.FinishTransaction(productDefinition, confirmedOrder.Info.TransactionID);
        }

        static bool IsValidPurchaseState(AppleInAppPurchaseReceipt? mostRecentReceipt, string productId)
        {
            var isValid = true;
            if (mostRecentReceipt != null)
            {
                var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType), mostRecentReceipt.productType.ToString());
                // if the product is auto-renewing subscription, check if this transaction is expired
                if (productType == AppleStoreProductType.AutoRenewingSubscription &&
                    new SubscriptionInfo(mostRecentReceipt, null).IsExpired() == Result.True)
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        bool IsRestored(string productId, AppleInAppPurchaseReceipt? productReceipt, string transactionId, string originalTransactionId)
        {
            bool isRestored;
            var currentProduct = ProductCache.Find(productId);
            if (currentProduct == null)
            {
                isRestored = false;
            }
            else if (currentProduct.definition.type == ProductType.Subscription)
            {
                isRestored = IsSubscriptionRestored(productReceipt, currentProduct);
            }
            else
            {
                isRestored = currentProduct.definition.type == ProductType.Subscription
                    ? IsSubscriptionRestored(productReceipt, currentProduct)
                    : IsNonSubscriptionRestored(transactionId, originalTransactionId);
            }

            return isRestored;
        }

        static bool IsSubscriptionRestored(AppleInAppPurchaseReceipt? productReceipt, Product previousProduct)
        {
            var isRestored = false;
            if (previousProduct.hasReceipt)
            {
                var subscriptionExpirationDate = productReceipt?.subscriptionExpirationDate;
                var subscriptionInfoHelper = new SubscriptionInfoHelper(previousProduct, null);
                var previousSubscriptionInfo = subscriptionInfoHelper.GetSubscriptionInfo();
                if (previousSubscriptionInfo != null &&
                    previousSubscriptionInfo.IsCancelled() == Result.False &&
                    previousSubscriptionInfo.GetExpireDate() >= subscriptionExpirationDate)
                {
                    isRestored = true;
                }
            }

            return isRestored;
        }

        static bool IsNonSubscriptionRestored(string transactionId, string? originalTransactionId)
        {
            return originalTransactionId != null && originalTransactionId != transactionId;
        }

        void UpdateAppleProductFields(string transactionId, string originalTransactionId, bool isRestored)
        {
            m_PendingOrders.TryGetValue(transactionId, out PendingOrder pendingOrder);
            m_ConfirmedOrders.TryGetValue(transactionId, out ConfirmedOrder confirmedOrder);

            if (pendingOrder != null)
            {
                pendingOrder.Info.Apple.IsRestored = isRestored;
                pendingOrder.Info.Apple.OriginalTransactionID = originalTransactionId;
            }

            if (confirmedOrder != null)
            {
                confirmedOrder.Info.Apple.IsRestored = isRestored;
                confirmedOrder.Info.Apple.OriginalTransactionID = originalTransactionId;
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
    }
}
