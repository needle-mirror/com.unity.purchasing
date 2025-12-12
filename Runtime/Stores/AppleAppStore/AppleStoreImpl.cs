#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using Purchasing.Utilities;
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
        bool m_RefreshAppReceipt = false;

        INativeAppleStore? m_Native;
        readonly IAppleFetchProductsService m_FetchProductsService;
        readonly ITransactionLog m_TransactionLog;

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
            if (StoreKitSelector.UseStoreKit1())
            {
                apple.Sk1SetUnityPurchasingCallback(Sk1MessageCallback);

            }
            else
            {
                apple.SetUnityPurchasingCallback(MessageCallback);
            }
        }

        public INativeAppleStore? GetNativeStore()
        {
            return m_Native;
        }

        public string? AppReceipt()
        {
            if (StoreKitSelector.UseStoreKit1())
            {
               return m_Native?.AppReceipt();

            }

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
            if (StoreKitSelector.UseStoreKit1())
            {
                OnPurchasesFetchedStoreKit1();
                return;
            }

            m_Native?.FetchExistingPurchases();
        }

        void OnPurchasesFetchedStoreKit1()
        {
            var receipt = AppReceipt();
            var appleReceipt = GetAppleReceiptFromBase64String(receipt);
            if (appleReceipt == null || !HasInAppPurchaseReceipts(appleReceipt))
            {
                return;
            }

            var products = ProductCache.GetProducts();

            var orders = CreateConfirmedOrdersForSK1(products, appleReceipt);
            PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
        }

        List<Order> CreateConfirmedOrdersForSK1(ReadOnlyObservableCollection<Product> products, AppleReceipt appleReceipt)
        {
            var orders = new List<Order>();
            // Enrich the product descriptions with parsed receipt data
            foreach (var product in products)
            {
                var mostRecentReceipt = FindMostRecentReceipt(appleReceipt, product.definition.storeSpecificId);
                if (mostRecentReceipt == null)
                {
                    continue;
                    //finalProductDescriptions.Add(productDescription);
                }

                var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType), mostRecentReceipt.productType.ToString());
                switch (productType)
                {
                    // if the product is auto-renewing subscription, filter the expired products
                    case AppleStoreProductType.AutoRenewingSubscription when new SubscriptionInfo(mostRecentReceipt, null).isExpired() == Result.True:
                        continue;
                    case AppleStoreProductType.Consumable:
                        // Nothing to do, a consumable with a receipt is Pending and will be sent to OnPurchaseSucceeded on launch
                        continue;
                    case AppleStoreProductType.AutoRenewingSubscription:
                    case AppleStoreProductType.NonConsumable:
                        UpdateAppleProductFields(product.definition.storeSpecificId,
                            mostRecentReceipt.originalTransactionIdentifier,
                            true);

                        var confirmedOrder = GenerateAppleConfirmedOrder(product.definition.id, mostRecentReceipt.transactionID, mostRecentReceipt.originalTransactionIdentifier, OwnershipType.Undefined, null, null, null);
                        orders.Add(confirmedOrder);
                        continue;
                    default:
                        continue;
                }
            }

            return orders;
        }

        static bool HasInAppPurchaseReceipts(AppleReceipt? appleReceipt)
        {
            return appleReceipt is {inAppPurchaseReceipts: {Length: > 0}};
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
            if (StoreKitSelector.UseStoreKit1())
            {
                m_Native?.SetApplicationUsername(value.ToString());
                return;
            }

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

        void OnPromotionalPurchaseAttempted(string productId)
        {
            if (OnPromotionalPurchaseIntercepted != null)
            {
                var product = ProductCache.Find(productId);
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

                var subscriptionInfo = TryGetSubscriptionInfoFromPayload(purchaseDetails);

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
                    orders.Add(GenerateApplePendingOrder(productId, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws, subscriptionInfo));
                }
                else
                {
                    orders.Add(GenerateAppleConfirmedOrder(productId, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws, subscriptionInfo));
                }
            }

            return orders;
        }

        static IAppleTransactionSubscriptionInfo? TryGetSubscriptionInfoFromPayload(Dictionary<string, object> purchaseDetails)
        {
            var productType = TryGetProductTypeForTransaction(purchaseDetails.TryGetString("productType"));
            if (productType is not AppleStoreProductType.AutoRenewingSubscription and not AppleStoreProductType.NonRenewingSubscription)
            {
                return null;
            }

            bool? isFree = null;
            var hasIsFree = purchaseDetails.TryGetValue("isFree", out var isFreeData);
            if (hasIsFree && isFreeData is bool isFreeBool)
            {
                isFree = isFreeBool;
            }

            var offerId = purchaseDetails.TryGetString("offerId");

            var offerType = OfferType.Unknown;
            var typeIntString = purchaseDetails.TryGetString("offerType");
            var parsedTypeInt = int.TryParse(typeIntString, out var offerInt) ? offerInt : -1;
            if (typeof(OfferType).IsEnumDefined(parsedTypeInt))
            {
                offerType = (OfferType)parsedTypeInt;
            }

            var expirationDate = TryGetDateTimeFromSecondsString(purchaseDetails.TryGetString("expirationDate"));
            var revocationDate = TryGetDateTimeFromSecondsString(purchaseDetails.TryGetString("revocationDate"));
            var purchaseDate = TryGetDateTimeFromSecondsString(purchaseDetails.TryGetString("purchaseDate"));

            return new AppleTransactionSubscriptionInfo(offerType, offerId, isFree, expirationDate, revocationDate, purchaseDate, productType);
        }

        static DateTime? TryGetDateTimeFromSecondsString(string? secondsString)
        {
            if (secondsString == null)
            {
                return null;
            }

            if (!double.TryParse(secondsString, out double seconds))
            {
                return null;
            }

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(seconds);
        }

        static AppleStoreProductType TryGetProductTypeForTransaction(string? productTypeString)
        {
            return productTypeString switch
            {
                "Consumable" => AppleStoreProductType.Consumable,
                "Non-Consumable" => AppleStoreProductType.NonConsumable,
                "Non-Renewing Subscription" => AppleStoreProductType.NonRenewingSubscription,
                "Auto-Renewable Subscription" => AppleStoreProductType.AutoRenewingSubscription,
                _ => AppleStoreProductType.Unknown
            };
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
            if (StoreKitSelector.UseStoreKit1())
            {
                var entitlementStatus = 0;
                var storeName = Application.platform == RuntimePlatform.OSXPlayer? MacAppStore.Name : AppleAppStore.Name;
                var orders = UnityIAPServices.Purchase(storeName).GetPurchases();
                switch (productDefinition.type)
                {
                    case ProductType.NonConsumable:
                    {
                        foreach (var order in orders)
                        {
                            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.storeSpecificId == productDefinition.storeSpecificId)
                            {
                                if (order is PendingOrder)
                                    entitlementStatus = 2;
                                else if (order is ConfirmedOrder)
                                    entitlementStatus = 1;
                                break;
                            }
                        }

                        break;
                    }
                    case ProductType.Subscription:
                    {
                        foreach (var order in orders)
                        {
                            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.storeSpecificId == productDefinition.storeSpecificId)
                            {
                                // Help me find the storeSpecificId in PurchasedProductInfo
                                foreach (var purchasedProductInfo in order.Info.PurchasedProductInfo)
                                {
                                    if (purchasedProductInfo.productId != productDefinition.storeSpecificId)
                                        continue;

                                    if (purchasedProductInfo.subscriptionInfo != null && purchasedProductInfo.subscriptionInfo.IsSubscribed() == Result.True)
                                    {
                                        if (order is PendingOrder)
                                            entitlementStatus = 2;
                                        else if (order is ConfirmedOrder)
                                            entitlementStatus = 1;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case ProductType.Consumable:
                    {
                        var isPending = orders.Any(order => order is PendingOrder && order.CartOrdered.Items().FirstOrDefault()?.Product.definition.storeSpecificId == productDefinition.storeSpecificId);
                        entitlementStatus = isPending? 2 : 0;
                        break;
                    }
                }

                OnCheckEntitlement(productDefinition.storeSpecificId, entitlementStatus);
                return;
            }
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

            var subscriptionInfo = TryGetSubscriptionInfoFromPayload(purchaseDetails);

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

            ProcessValidPurchase(productId, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws, subscriptionInfo);
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

        void ProcessValidPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string signatureJws, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            if (!m_TransactionLog.HasRecordOf(transactionId))
            {
                ProcessNewPurchase(id, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws, subscriptionInfo);
            }
            else
            {
                ProcessLoggedPurchase(id, transactionId, originalTransactionId, expirationDate, ownershipType, appAccountToken, signatureJws, subscriptionInfo);
            }
        }

        void ProcessNewPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string signatureJws, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            var pendingOrder = GenerateApplePendingOrder(id, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws, subscriptionInfo);
            PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
        }

        void ProcessLoggedPurchase(string id, string transactionId, string originalTransactionId, string expirationDate, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            var confirmedOrder = GenerateAppleConfirmedOrder(id, transactionId, originalTransactionId, ownershipType, appAccountToken, signatureJws, subscriptionInfo);
            EnsureConfirmedOrderIsFinished(confirmedOrder);
        }

        DeferredOrder GenerateAppleDeferredOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new DeferredOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws));
        }

        PendingOrder GenerateApplePendingOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new PendingOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws), subscriptionInfo);
        }

        ConfirmedOrder GenerateAppleConfirmedOrder(string id, string transactionID, string originalTransactionId, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws, IAppleTransactionSubscriptionInfo? subscriptionInfo)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new ConfirmedOrder(cart, new AppleOrderInfo(transactionID, m_StoreName, this, originalTransactionId, ownershipType, appAccountToken, signatureJws), subscriptionInfo);
        }

        void EnsureConfirmedOrderIsFinished(ConfirmedOrder confirmedOrder)
        {
            var productDefinition = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition;
            InvokeDuplicateTransactionError(confirmedOrder);
            base.FinishTransaction(productDefinition, confirmedOrder.Info.TransactionID);
        }

        void InvokeDuplicateTransactionError(ConfirmedOrder confirmedOrder)
        {
            var failedOrder = new FailedOrder(confirmedOrder.CartOrdered,
                PurchaseFailureReason.DuplicateTransaction,
                "Purchase has already been confirmed.");

            PurchaseCallback?.OnPurchaseFailed(failedOrder);
        }

#region StoreKit1
    [MonoPInvokeCallback(typeof(UnityPurchasingCallback))]
        static void Sk1MessageCallback(string subject, string payload, string receipt, string transactionId, string originalTransactionId, bool isRestored)
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
                    m_FetchProductsService.OnProductsFetched(payload);
                    break;
                case "OnPurchaseSucceeded":
                    OnPurchaseSucceeded(payload, receipt, transactionId, originalTransactionId, isRestored);
                    break;
                case "OnPurchaseFailed":
                    OnPurchaseFailedSk1(payload);
                    break;
                case "onProductPurchaseDeferred":
                    OnPurchaseDeferredSk1(payload);
                    break;
                case "onPromotionalPurchaseAttempted":
                    OnPromotionalPurchaseAttempted(payload);
                    break;
                case "onFetchStorePromotionOrderSucceeded":
                    OnFetchStorePromotionOrderSucceeded(payload);
                    break;
                case "onFetchStorePromotionOrderFailed":
                    OnFetchStorePromotionOrderFailed("");
                    break;
                case "onFetchStorePromotionVisibilitySucceeded":
                    OnFetchStorePromotionVisibilitySucceeded(payload);
                    break;
                case "onFetchStorePromotionVisibilityFailed":
                    OnFetchStorePromotionVisibilityFailed("");
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
                    OnEntitlementsRevokedSk1(payload);
                    break;
            }
        }

        void OnPurchaseSucceeded(string id, string receipt, string transactionId, string originalTransactionId, bool isRestored)
        {
            var appleReceipt = GetAppleReceiptFromBase64String(receipt);
            var mostRecentReceipt = FindMostRecentReceipt(appleReceipt, id);

            if (IsValidPurchaseStateSk1(mostRecentReceipt))
            {
                isRestored = isRestored || IsRestored(id, mostRecentReceipt, transactionId, originalTransactionId);
                UpdateAppleProductFields(id, originalTransactionId, isRestored);

                if (!m_TransactionLog.HasRecordOf(transactionId))
                {
                    ProcessNewPurchase(id, transactionId, originalTransactionId, string.Empty, OwnershipType.Undefined, null, string.Empty, null);
                }
                else
                {
                    ProcessLoggedPurchase(id, transactionId, originalTransactionId, string.Empty, OwnershipType.Undefined, null, string.Empty, null);
                }
            }
            else
            {
                base.FinishTransaction(null, transactionId);
            }
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

        static AppleReceipt? GetAppleReceiptFromBase64String(string? receipt)
        {
            AppleReceipt? appleReceipt = null;
            if (!string.IsNullOrEmpty(receipt))
            {
                var parser = new AppleReceiptParser();
                try
                {
                    appleReceipt = parser.Parse(Convert.FromBase64String(receipt));
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return appleReceipt;
        }

        static bool IsValidPurchaseStateSk1(AppleInAppPurchaseReceipt? mostRecentReceipt)
        {
            var isValid = true;
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
            return isValid;
        }

        bool IsRestored(string productId, AppleInAppPurchaseReceipt? productReceipt, string transactionId, string originalTransactionId)
        {
            bool isRestored;

            var currentProduct = FindProductById(productId);
            if (currentProduct.definition.type == ProductType.Unknown)
            {
                isRestored = false;
            }
            else
            {
                isRestored = currentProduct.definition.type == ProductType.Subscription
                    ? IsSubscriptionRestored(productReceipt, currentProduct)
                    : IsNonSubscriptionRestored(transactionId, originalTransactionId);
            }

            static bool IsSubscriptionRestored(AppleInAppPurchaseReceipt? productReceipt, Product previousProduct)
            {
                var isRestored = false;
                if (previousProduct.hasReceipt)
                {
                    var subscriptionExpirationDate = productReceipt?.subscriptionExpirationDate;
                    var subscriptionManager = new SubscriptionManager(previousProduct, null);
                    var previousSubscriptionInfo = subscriptionManager.getSubscriptionInfo();
                    if (previousSubscriptionInfo != null &&
                        previousSubscriptionInfo.isCancelled() == Result.False &&
                        previousSubscriptionInfo.getExpireDate() >= subscriptionExpirationDate)
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

            return isRestored;
        }

        void UpdateAppleProductFields(string id, string originalTransactionId, bool isRestored)
        {
            var product = FindProductById(id);
            if (product.definition.type != ProductType.Unknown)
            {
                product.appleProductIsRestored = isRestored;
                product.appleOriginalTransactionID = originalTransactionId;
            }
        }

        void OnPurchaseFailedSk1(string json)
        {
            try
            {
                var purchaseFailureDescription = JSONSerializer.DeserializeFailureReason(json, ProductCache);
                OnPurchaseFailed(purchaseFailureDescription, json);
            }
            catch
            {
                OnPurchaseFailed(new PurchaseFailureDescription(Product.CreateUnknownProduct("Unknown ProductID"), PurchaseFailureReason.Unknown, "Unable to parse purchase failure details"));
            }
        }

        void OnPurchaseDeferredSk1(string productId)
        {
            var product = FindProductById(productId);
            if (product.definition.type != ProductType.Unknown)
            {
                Guid? appAccountToken = null; // passed as null as there is only a promise of a purchase
                var deferredOrder = GenerateAppleDeferredOrder(product.definition.storeSpecificId, "", "", OwnershipType.Undefined, appAccountToken, null);
                PurchaseCallback?.OnPurchaseDeferred(deferredOrder);
            }
        }

        void OnEntitlementsRevokedSk1(string productIds)
        {
            var productIdList = productIds.ArrayListFromJson();

            foreach (string productId in productIdList)
            {
                RevokeEntitlement(productId);
            }

        }

#endregion StoreKit1
    }
}
