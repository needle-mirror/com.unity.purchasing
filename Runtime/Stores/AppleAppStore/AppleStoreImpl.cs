#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AOT;
using Uniject;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// App Store implementation of <see cref="IStore"/>.
    /// </summary>
    class AppleStoreImpl : JsonStore, IAppleStoreCallbacks, IAppleAppReceiptViewer
    {
        Action<bool>? m_ObsoleteRestoreCallback;
        Action<bool, string?>? m_RestoreCallback;
        Action<string>? m_FetchStorePromotionOrderError;
        Action<List<Product>>? m_FetchStorePromotionOrderSuccess;
        Action<Product>? m_PromotionalPurchaseCallback;
        Action<string>? m_FetchStorePromotionVisibilityError;
        Action<string, AppleStorePromotionVisibility>? m_FetchStorePromotionVisibilitySuccess;
        INativeAppleStore? m_Native;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;
        readonly IAppleRetrieveProductsService m_RetrieveProductsService;
        readonly ITransactionLog m_TransactionLog;
        static HashSet<string> s_SubscriptionDeduplicationData = new();

        static IUtil? s_Util;
        static AppleStoreImpl? s_Instance;

        bool m_IsTransactionObserverEnabled;
        Guid m_appAccountToken;

        protected AppleStoreImpl(ICartValidator cartValidator, IAppleRetrieveProductsService retrieveProductsService,
            ITransactionLog transactionLog,
            IUtil util,
            ILogger logger,
            ITelemetryDiagnostics telemetryDiagnostics)
            : base(cartValidator, logger, DefaultStoreHelper.GetDefaultStoreName())
        {
            m_appAccountToken = Guid.Empty;
            s_Util = util;
            s_Instance = this;
            m_TelemetryDiagnostics = telemetryDiagnostics;
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

        public string? AppReceipt()
        {
            return m_Native?.AppReceipt();
        }

        public override void Connect()
        {
            m_Native?.Connect();
            ConnectCallback?.OnStoreConnectionSucceeded();
        }

        protected override void FinishTransaction(ProductDefinition? productDefinition, string transactionId)
        {
            m_TransactionLog.Record(transactionId);
            base.FinishTransaction(productDefinition, transactionId);
        }

        public override void Purchase(ICart cart)
        {
            m_CartValidator.Validate(cart);
            var productDefinition = cart.Items().First().Product.definition;
            var purchaseOptions = PurchaseOptions();
            Purchase(productDefinition, purchaseOptions);
        }

        string PurchaseOptions()
        {
            var options = new Dictionary<string, object>
            {
                { "simulatesAskToBuyInSandbox", simulateAskToBuy },
                { "appAccountToken", m_appAccountToken },
            };

            return Json.Serialize(options);
        }

        public void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback)
        {
            m_PromotionalPurchaseCallback = callback;
        }

        public override async void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            try
            {
                var productDescriptions = await m_RetrieveProductsService.RetrieveProducts(products);

                ProductsCallback?.OnProductsRetrieved(productDescriptions);

                if (!m_IsTransactionObserverEnabled)
                {
                    m_Native?.AddTransactionObserver();
                    m_IsTransactionObserverEnabled = true;
                }

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

        public override void FetchPurchases()
        {
            m_Native?.FetchExistingPurchases();
        }

        public void SetFetchStorePromotionOrderCallbacks(Action<List<Product>> successCallback, Action<string> errorCallback)
        {
            m_FetchStorePromotionOrderError = errorCallback;
            m_FetchStorePromotionOrderSuccess = successCallback;
        }

        public void SetFetchStorePromotionVisibilityCallbacks(Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)
        {
            m_FetchStorePromotionVisibilityError = errorCallback;
            m_FetchStorePromotionVisibilitySuccess = successCallback;
        }

        public void SetRestoreTransactionsCallback(Action<bool, string?> successCallback)
        {
            m_RestoreCallback = successCallback;
        }

        public void ClearTransactionLog()
        {
            m_TransactionLog.Clear();
        }

        public bool simulateAskToBuy { get; set; }
        public void SetAppAccountToken(Guid value)
        {
            m_appAccountToken = value;
        }

        public override void OnPurchaseDeferred(string productDetails)
        {
            var productDescriptions = JSONSerializer.DeserializeProductDescriptionsFromFetchProductsSk2(productDetails);
            var productDescription = productDescriptions.FirstOrDefault();
            if (productDescription != null)
            {
                Guid? appAccountToken = null; // passed as null as there is only a promise of a purchase
                var deferredOrder = GenerateAppleDeferredOrder(productDescription.storeSpecificId, productDescription.transactionId, "", OwnershipType.Undefined, appAccountToken, null);
                PurchaseCallback?.OnPurchaseDeferred(deferredOrder);
            }
        }

        void OnPromotionalPurchaseAttempted(IntPtr productIdPtr)
        {
            var productId = "";
            if (productIdPtr != IntPtr.Zero)
            {
                productId = Marshal.PtrToStringAuto(productIdPtr) ?? string.Empty;

                // Deallocate the memory when done
                m_Native?.DeallocateMemory(productIdPtr);
            }

            if (null != m_PromotionalPurchaseCallback)
            {
                var product = ProductCache.Find(productId);
                if (null != product)
                {
                    m_PromotionalPurchaseCallback(product);
                }
            }
        }

        public override void OnPurchasesFetched(string json)
        {
            var fetchedPurchases = JSONSerializer.DeserializeFetchedPurchases(json);
            var orders = CreateOrdersFromFetchedPurchases(fetchedPurchases);
            PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
        }

        List<Order> CreateOrdersFromFetchedPurchases(Dictionary<string, Dictionary<string, object>> fetchedPurchases)
        {
            var pendingOrders = new List<Order>();
            if (fetchedPurchases.TryGetValue("unfinishedTransactions", out var unfinishedTransactions))
            {
                pendingOrders = GenerateOrdersFromProducts(unfinishedTransactions, true);
            }

            var confirmedOrders = new List<Order>();
            if (fetchedPurchases.TryGetValue("finishedTransactions", out var finishedTransactions))
            {
                confirmedOrders = GenerateOrdersFromProducts(finishedTransactions, false);
            }

            return pendingOrders.Concat(confirmedOrders).ToList();
        }

        List<Order> GenerateOrdersFromProducts(Dictionary<string, object> transactions, bool isPending)
        {
            var orders = new List<Order>();
            foreach (var transaction in transactions)
            {
                if (transaction.Value is not Dictionary<string, object> purchaseDetails)
                {
                    continue;
                }

                var transactionId = purchaseDetails.TryGetString("transactionId");
                var originalTransactionId = purchaseDetails.TryGetString("originalTransactionId");

                var ownershipType = OwnershipTypeFromString(purchaseDetails.TryGetString("ownershipType"));
                var appAccountTokenString = purchaseDetails.TryGetString("appAccountToken");
                var signatureJws = purchaseDetails.TryGetString("signatureJws");
                Guid? appAccountToken = null;
                if (Guid.TryParse(appAccountTokenString, out Guid parsedToken))
                {
                    appAccountToken = parsedToken;
                }

                if (isPending)
                {
                    orders.Add(GenerateApplePendingOrder(transaction.Key, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws));
                }
                else
                {
                    orders.Add(GenerateAppleConfirmedOrder(transaction.Key, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws));
                }
            }

            return orders;
        }

        static OwnershipType OwnershipTypeFromString(string ownershipTypeString)
        {
            return ownershipTypeString switch
            {
                "PURCHASED" => OwnershipType.Purchased,
                "FAMILY_SHARED" => OwnershipType.FamilyShared,
                _ => OwnershipType.Undefined
            };
        }

        void OnTransactionsRestoredSuccess()
        {
            m_RestoreCallback?.Invoke(true, null);
        }

        public void OnTransactionsRestoredFail(string error)
        {
            m_RestoreCallback?.Invoke(false, error);
        }

        void OnEntitlementRevoked(string purchaseDetailsJson)
        {
            var purchaseDetails = JSONSerializer.DeserializePurchaseDetails(purchaseDetailsJson);
            var productId = purchaseDetails.TryGetString("productId");

            RevokeEntitlement(productId);
        }

        void RevokeEntitlement(string productId)
        {
            EntitlementRevokedCallback?.onEntitlementRevoked(productId);
        }

        void OnFetchStorePromotionOrderSucceeded(string productIds)
        {
            if (null != m_FetchStorePromotionOrderSuccess)
            {
                var productIdList = productIds.ArrayListFromJson();
                var products = new List<Product>();

                if (productIdList != null)
                {
                    foreach (var productId in productIdList)
                    {
                        var product = ProductCache.FindOrDefault(productId as string);

                        products.Add(product);
                    }
                }

                m_FetchStorePromotionOrderSuccess(products);
            }
        }

        void OnFetchStorePromotionOrderFailed(string error)
        {
            m_FetchStorePromotionOrderError?.Invoke(error);
        }

        void OnFetchStorePromotionVisibilitySucceeded(string result)
        {
            if (null != m_FetchStorePromotionVisibilitySuccess)
            {
                var resultDictionary = (
                    Json.Deserialize(result) as Dictionary<string, object>
                )?.ToDictionary(k => k.Key, k => k.Value.ToString());

                var productId = resultDictionary?["productId"] ?? string.Empty;
                var storePromotionVisibility = resultDictionary?["visibility"];
                Enum.TryParse(storePromotionVisibility, out AppleStorePromotionVisibility visibility);
                m_FetchStorePromotionVisibilitySuccess(productId, visibility);
            }
        }

        void OnFetchStorePromotionVisibilityFailed(string error)
        {
            m_FetchStorePromotionVisibilityError?.Invoke(error);
        }

        [MonoPInvokeCallback(typeof(UnityPurchasingCallback))]
        static void MessageCallback(string subject, string payload, int entitlementStatus, IntPtr pointer)
        {
            s_Util?.RunOnMainThread(() =>
            {
                s_Instance?.ProcessCallbackMessage(subject, payload, entitlementStatus, pointer);
            });
        }

        void ProcessCallbackMessage(string subject, string payload, int entitlementStatus, IntPtr pointer)
        {
            switch (subject)
            {
                case "OnProductsRetrieved":
                    m_RetrieveProductsService.OnProductsRetrieved(payload);
                    break;
                case "OnProductsRetrieveFailed":
                    m_RetrieveProductsService.OnProductDetailsRetrieveFailed(payload);
                    break;
                case "OnPurchaseSucceeded":
                    OnPurchaseSucceeded(payload);
                    break;
                case "OnPurchaseFailed":
                    OnPurchaseFailed(payload);
                    break;
                case "OnPurchasesFetched":
                    OnPurchasesFetched(payload);
                    break;
                case "OnPurchaseDeferred":
                    OnPurchaseDeferred(payload);
                    break;
                case "OnPromotionalPurchaseAttempted":
                    OnPromotionalPurchaseAttempted(pointer);
                    break;
                case "OnFetchStorePromotionOrderSucceeded":
                    OnFetchStorePromotionOrderSucceeded(payload);
                    break;
                case "OnFetchStorePromotionOrderFailed":
                    OnFetchStorePromotionOrderFailed(payload);
                    break;
                case "OnFetchStorePromotionVisibilitySucceeded":
                    OnFetchStorePromotionVisibilitySucceeded(payload);
                    break;
                case "OnFetchStorePromotionVisibilityFailed":
                    OnFetchStorePromotionVisibilityFailed(payload);
                    break;
                case "OnTransactionsRestoredSuccess":
                    OnTransactionsRestoredSuccess();
                    break;
                case "OnTransactionsRestoredFail":
                    OnTransactionsRestoredFail(payload);
                    break;
                case "OnEntitlementRevoked":
                    OnEntitlementRevoked(payload);
                    break;
                case "OnCheckEntitlement":
                    OnCheckEntitlement(payload, entitlementStatus);
                    break;
            }
        }

        public override void CheckEntitlement(ProductDefinition productDefinition)
        {
            m_Native?.CheckEntitlement(productDefinition.storeSpecificId);
        }

        void OnCheckEntitlement(string productId, int entitlementStatus)
        {
            var product = ProductCache.FindOrDefault(productId);
            if (entitlementStatus != 0)
            {
                switch (product.definition.type)
                {
                    case ProductType.Consumable:
                        EntitlementCallback?.OnCheckEntitlementSucceeded(product.definition, EntitlementStatus.EntitledUntilConsumed);
                        return;
                    case ProductType.Subscription:
                    case ProductType.NonConsumable:
                        EntitlementCallback?.OnCheckEntitlementSucceeded(product.definition, entitlementStatus == 1 ? EntitlementStatus.FullyEntitled : EntitlementStatus.EntitledButNotFinished);
                        return;
                }
            }

            EntitlementCallback?.OnCheckEntitlementSucceeded(product.definition, EntitlementStatus.NotEntitled);
        }

        void OnPurchaseSucceeded(string purchaseDetailsJson)
        {
            var purchaseDetails = JSONSerializer.DeserializePurchaseDetails(purchaseDetailsJson);

            var productId = purchaseDetails.TryGetString("productId");
            var transactionId = purchaseDetails.TryGetString("transactionId");
            var originalTransactionId = purchaseDetails.TryGetString("originalTransactionId");
            var expirationDate = purchaseDetails.TryGetString("expirationDate");
            var ownershipType = OwnershipTypeFromString(purchaseDetails.TryGetString("ownershipType"));
            var appAccountTokenString = purchaseDetails.TryGetString("appAccountToken");
            var signatureJws = purchaseDetails.TryGetString("signatureJws");

            Guid? appAccountToken = null;
            if (Guid.TryParse(appAccountTokenString, out Guid parsedToken))
            {
                appAccountToken = parsedToken;
            }

            if (IsValidPurchaseState(expirationDate, productId))
            {
                ProcessValidPurchase(productId, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws);
            }
            else
            {
                base.FinishTransaction(null, transactionId);
            }
        }

        void ProcessValidPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string signatureJws)
        {
            if (!m_TransactionLog.HasRecordOf(transactionId))
            {
                ProcessNewPurchase(id, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws);
            }
            else
            {
                ProcessLoggedPurchase(id, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws);
            }
        }

        void ProcessNewPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string signatureJws)
        {
            var pendingOrder = GenerateApplePendingOrder(id, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws);
            PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
            AddSubscriptionDeduplicationData(id, expirationDate);
        }

        void ProcessLoggedPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var confirmedOrder = GenerateAppleConfirmedOrder(id, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws);
            EnsureConfirmedOrderIsFinished(confirmedOrder);
            AddSubscriptionDeduplicationData(id, expirationDate);
        }

        void AddSubscriptionDeduplicationData(string productId, string expirationDate)
        {
            var product = FindProductById(productId);
            if (product.definition.type == ProductType.Subscription)
            {
                s_SubscriptionDeduplicationData.Add(productId + "|" + expirationDate);
            }
        }

        DeferredOrder GenerateAppleDeferredOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new DeferredOrder(cart, new AppleOrderInfo(transactionID, m_storeName, this, "", ownershipType, appAccountToken, signatureJws));
        }

        PendingOrder GenerateApplePendingOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new PendingOrder(cart, new AppleOrderInfo(transactionID, m_storeName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
        }

        ConfirmedOrder GenerateAppleConfirmedOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new ConfirmedOrder(cart, new AppleOrderInfo(transactionID, m_storeName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
        }

        void EnsureConfirmedOrderIsFinished(ConfirmedOrder confirmedOrder)
        {
            var productDefinition = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition;
            base.FinishTransaction(productDefinition, confirmedOrder.Info.TransactionID);
        }

        bool IsValidPurchaseState(string expirationDate, string productId)
        {
            var product = FindProductById(productId);
            if (product.definition.type == ProductType.Subscription)
            {
                return !s_SubscriptionDeduplicationData.Contains(productId + "|" + expirationDate);
            }

            return true;
        }
    }
}
