#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        Action<string>? m_FetchStorePromotionVisibilityError;
        Action<string, AppleStorePromotionVisibility>? m_FetchStorePromotionVisibilitySuccess;

        // TODO: IAP-3929
        Action<string>? m_RefreshAppReceiptSuccessCallback;
        Action<string>? m_RefreshAppReceiptErrorCallback;
        TaskCompletionSource<bool>? m_RefreshAppReceiptTask;
        bool m_RefreshAppReceipt = true;

        INativeAppleStore? m_Native;
        readonly IAppleFetchProductsService m_FetchProductsService;
        readonly ITransactionLog m_TransactionLog;
        static HashSet<string> s_SubscriptionDeduplicationData = new();

        static IUtil? s_Util;
        static AppleStoreImpl? s_Instance;

        string? appReceipt;

        bool m_IsTransactionObserverEnabled;
        Guid m_AppAccountToken;

        public event Action<Product>? OnPromotionalPurchaseIntercepted;

        protected AppleStoreImpl(ICartValidator cartValidator, IAppleFetchProductsService fetchProductsService,
            ITransactionLog transactionLog,
            IUtil util,
            ILogger logger,
            ITelemetryDiagnostics telemetryDiagnostics)
            : base(cartValidator, logger, AppleAppStore.Name)
        {
            m_AppAccountToken = Guid.Empty;
            s_Util = util;
            s_Instance = this;
            m_FetchProductsService = fetchProductsService;
            m_TransactionLog = transactionLog;
        }
        public void SetNativeStore(INativeAppleStore apple)
        {
            base.SetNativeStore(apple);
            m_Native = apple;
            m_FetchProductsService.SetNativeStore(apple);

            //TODO: IAP-4089: Add test
            Application.quitting += () => apple.SetUnityPurchasingCallback(null);
            apple.SetUnityPurchasingCallback(MessageCallback);
        }

        public INativeAppleStore? GetNativeStore()
        {
            return m_Native;
        }

        public string? AppReceipt()
        {
            return appReceipt;
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
                { "appAccountToken", m_AppAccountToken },
            };

            return Json.Serialize(options);
        }

        public override async void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            try
            {
                var productDescriptions = await m_FetchProductsService.FetchProducts(products);

                ProductsCallback?.OnProductsFetched(productDescriptions);

                if (!m_IsTransactionObserverEnabled)
                {
                    m_Native?.AddTransactionObserver();
                    m_IsTransactionObserverEnabled = true;
                }

                // If there is a promotional purchase callback, tell the store to intercept those purchases.
                if (OnPromotionalPurchaseIntercepted != null)
                {
                    m_Native?.InterceptPromotionalPurchases();
                }
            }
            catch (FetchProductsException exception)
            {
                ProductsCallback?.OnProductsFetchFailed(exception.FailureDescription);
            }
            catch (Exception e)
            {
                ProductsCallback?.OnProductsFetchFailed(new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown, e.Message));
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

        public void SetRestoreTransactionsCallback(Action<bool, string?>? successCallback)
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
            m_AppAccountToken = value;
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

        void OnPromotionalPurchaseAttempted(string payload)
        {
            if (OnPromotionalPurchaseIntercepted != null)
            {
                var product = ProductCache.Find(payload);
                if (null != product)
                {
                    OnPromotionalPurchaseIntercepted(product);
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
                var productId = purchaseDetails.TryGetString("productId");
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
                    orders.Add(GenerateApplePendingOrder(productId, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws));
                }
                else
                {
                    orders.Add(GenerateAppleConfirmedOrder(productId, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws));
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

        void OnTransactionsRestoredFail(string error)
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
        static void MessageCallback(IntPtr subjectPtr, IntPtr payloadPtr, int entitlementStatus)
        {
            s_Util?.RunOnMainThread(() =>
            {
                s_Instance?.ProcessCallbackMessage(subjectPtr, payloadPtr, entitlementStatus);
            });
        }

        void ProcessCallbackMessage(IntPtr subjectPtr, IntPtr payloadPtr, int entitlementStatus)
        {
            var subject = ConvertPtrToString(subjectPtr);
            var payload = ConvertPtrToString(payloadPtr);

            switch (subject)
            {
                case "OnProductsFetched":
                    m_FetchProductsService.OnProductsFetched(payload);
                    break;
                case "OnProductsFetchFailed":
                    m_FetchProductsService.OnProductDetailsRetrieveFailed(payload);
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
                    OnPromotionalPurchaseAttempted(payload);
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
                // TODO: IAP-3929
                case "onAppReceiptRefreshed":
                    OnAppReceiptRetrieved(payload);
                    break;
                // TODO: IAP-3929
                case "onAppReceiptRefreshFailed":
                    OnAppReceiptRefreshedFailed(payload);
                    break;
            }
        }

        string ConvertPtrToString(IntPtr subjectPtr)
        {
            var subject = "";
            if (subjectPtr != IntPtr.Zero)
            {
                subject = Marshal.PtrToStringAuto(subjectPtr) ?? string.Empty;

                // Deallocate the memory when done
                m_Native?.DeallocateMemory(subjectPtr);
            }

            return subject;
        }

        // TODO: IAP-3929
        public void SetRefreshAppReceiptCallbacks(Action<string> successCallback, Action<string> errorCallback)
        {
            m_RefreshAppReceiptSuccessCallback = successCallback;
            m_RefreshAppReceiptErrorCallback = errorCallback;
        }

        // TODO: IAP-3929
        public void SetRefreshAppReceipt(bool refreshAppReceipt)
        {
            m_RefreshAppReceipt = refreshAppReceipt;
        }

        // TODO: IAP-3929
        void OnAppReceiptRetrieved(string receipt)
        {
            appReceipt = receipt;
            m_RefreshAppReceiptTask?.TrySetResult(true);
            m_RefreshAppReceiptSuccessCallback?.Invoke(receipt);
        }

        // TODO: IAP-3929
        void OnAppReceiptRefreshedFailed(string error)
        {
            m_RefreshAppReceiptTask?.TrySetResult(false);
            m_RefreshAppReceiptErrorCallback?.Invoke(error);
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
                        EntitlementCallback?.OnCheckEntitlement(product.definition, EntitlementStatus.EntitledUntilConsumed);
                        return;
                    case ProductType.Subscription:
                    case ProductType.NonConsumable:
                        EntitlementCallback?.OnCheckEntitlement(product.definition, entitlementStatus == 1 ? EntitlementStatus.FullyEntitled : EntitlementStatus.EntitledButNotFinished);
                        return;
                }
            }

            EntitlementCallback?.OnCheckEntitlement(product.definition, EntitlementStatus.NotEntitled);
        }

        async void OnPurchaseSucceeded(string purchaseDetailsJson)
        {
            // TODO: IAP-3929 - Remove RefreshAppReceipt
            if (m_RefreshAppReceipt)
            {
                try
                {
                    await RefreshAppReceiptAsync();
                }
                catch (Exception e)
                {
                    OnAppReceiptRefreshedFailed(e.Message);
                }
                finally
                {
                    m_RefreshAppReceiptTask = null;
                }
            }

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

        // TODO: IAP-3929
        Task<bool> RefreshAppReceiptAsync()
        {
            if (m_RefreshAppReceiptTask != null)
            {
                return m_RefreshAppReceiptTask.Task;
            }

            m_RefreshAppReceiptTask = new TaskCompletionSource<bool>();

            m_Native?.RefreshAppReceipt();

            return m_RefreshAppReceiptTask.Task;
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
            return new DeferredOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
        }

        PendingOrder GenerateApplePendingOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new PendingOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
        }

        ConfirmedOrder GenerateAppleConfirmedOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new ConfirmedOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
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
