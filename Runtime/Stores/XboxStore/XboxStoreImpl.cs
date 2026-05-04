#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using Purchasing.Extension;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class XboxStoreImpl : InternalStore
    {
        private XUserHandle _userHandle;
        private XStoreContext _storeContext = null;
        private XUserChangeRegistrationToken _registrationToken;

        private const XStoreProductKind k_AllProductKinds = XStoreProductKind.Consumable | XStoreProductKind.UnmanagedConsumable | XStoreProductKind.Durable | XStoreProductKind.Game | XStoreProductKind.Pass;

        public delegate void ShowProductPageUICallback(int hResult, string storeSpecificId);

        protected readonly ILogger Logger;
        private readonly IXboxFetchProductsService m_FetchProductsService;
        private readonly IXboxQueryEntitlementsService m_QueryEntitlementsService;
        private readonly IXboxPurchaseService m_PurchaseService;

        [Preserve]
        internal XboxStoreImpl(ILogger logger, IXboxFetchProductsService fetchProductsService, IXboxQueryEntitlementsService queryEntitlementsService, IXboxPurchaseService purchaseService)
        {
            Logger = logger;
            m_FetchProductsService = fetchProductsService;
            m_QueryEntitlementsService = queryEntitlementsService;
            m_PurchaseService = purchaseService;
        }

        ~XboxStoreImpl()
        {
            if (_userHandle != null)
            {
                SDK.XUserCloseHandle(_userHandle);
                _userHandle = null;
            }

            if (_storeContext != null)
            {
                SDK.XStoreCloseContextHandle(_storeContext);
                _storeContext = null;
            }

            SDK.XUserUnregisterForChangeEvent(_registrationToken);
        }

#region Connect
        public override void Connect()
        {
            AddUser();
            SDK.XUserRegisterForChangeEvent(UserChangeEventCallback, out _registrationToken);
        }

        private void AddUser()
        {
            SDK.XUserAddAsync(XUserAddOptions.AddDefaultUserAllowingUI, AddUserComplete);
        }

        private void AddUserComplete(int hResult, XUserHandle userHandle)
        {
            if (HR.FAILED(hResult))
            {
                ConnectCallback?.OnStoreConnectionFailed(new StoreConnectionFailureDescription($"Could not sign the user in, hr=0x{hResult:X} ({HR.NameOf(hResult)})"));
                return;
            }

            _userHandle = userHandle;
            CompletePostSignInInitialization();
        }

        private void UserChangeEventCallback(IntPtr _, XUserLocalId userLocalId, XUserChangeEvent eventType)
        {
            if (eventType == XUserChangeEvent.SignedOut)
            {
                // ULO-8335 Support user change events
                Debug.unityLogger.LogIAPWarning("User signed out");

                if (_userHandle != null)
                {
                    SDK.XUserCloseHandle(_userHandle);
                    _userHandle = null;
                }

                if (_storeContext != null)
                {
                    SDK.XStoreCloseContextHandle(_storeContext);
                    _storeContext = null;
                }

                AddUser();
            }
        }

        private void CompletePostSignInInitialization()
        {
            string gamertag = string.Empty;

            int hResult = SDK.XUserGetGamertag(_userHandle, XUserGamertagComponent.UniqueModern, out gamertag);
            if (HR.FAILED(hResult))
            {
                var message = $"Failed to get Gamertag, hr=0x{hResult:X}";
                Debug.unityLogger.LogIAPWarning(message);
                ConnectCallback?.OnStoreConnectionFailed(new StoreConnectionFailureDescription(message));
                return;
            }

#if UNITY_GAMECORE && !UNITY_EDITOR
            hResult = SDK.XStoreCreateContext(_userHandle, out _storeContext);
#else
            // We do not need to pass a user on standalone PC as the currently signed in store user will be used.
            hResult = SDK.XStoreCreateContext(out _storeContext);
#endif
            if (HR.FAILED(hResult))
            {
                var message = $"Failed to create XStoreContext, hr=0x{hResult:X}";
                Debug.unityLogger.LogIAPError(message);
                ConnectCallback?.OnStoreConnectionFailed(new StoreConnectionFailureDescription(message));
                return;
            }

            ConnectCallback?.OnStoreConnectionSucceeded();
        }
#endregion

#region FetchProducts
        public void FetchAvailableProducts()
        {
            // This will not get all the products in Unity's catalog. Instead, this is fetching all available products from the Xbox catalog
            m_FetchProductsService.FetchAvailableProducts(_storeContext, k_AllProductKinds, FetchProductsCallbackFunction);
        }

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            var productIds = products.Select(p => p.storeSpecificId).ToArray();
            m_FetchProductsService.FetchProducts(_storeContext, k_AllProductKinds, productIds, FetchProductsCallbackFunction);
        }

        private void FetchProductsCallbackFunction(int hResult, List<XStoreProduct> xStoreProducts)
        {
            if (HR.FAILED(hResult))
            {
                var message = $"[GDK] Failed to get products. hr=0x{hResult:X}";
                var description = new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown, message);
                ProductsCallback?.OnProductsFetchFailed(description);
                return;
            }

            var productDescriptions = new List<ProductDescription>(xStoreProducts.Count);
            foreach (var xStoreProduct in xStoreProducts)
            {
                productDescriptions.Add(ConvertToProductDescription(xStoreProduct));
            }

            ProductsCallback?.OnProductsFetched(productDescriptions);
        }
#endregion

#region FetchPurchases
        public override void FetchPurchases()
        {
            m_QueryEntitlementsService.QueryEntitlementsAsync(_storeContext, k_AllProductKinds, null, GetEntitledProductsAsyncCallback);
        }

        private void GetEntitledProductsAsyncCallback(int hResult, List<XStoreProduct> entitledProducts, ProductDefinition productToCheck = null)
        {
            if (HR.FAILED(hResult))
            {
                PurchaseFetchCallback?.OnPurchasesRetrievalFailed(CreatePurchasesFetchFailureDescription(hResult));
                return;
            }

            var orders = new List<Order>();
            foreach (var xStoreProduct in entitledProducts)
            {
                var product = FindProduct(xStoreProduct.StoreId) ?? ConvertToUnknownProduct(xStoreProduct);
                var cart = new Cart(product);
                // ULO-9664 Track Receipts and Transaction IDs
                var receipt = "receipt";
                var info = new OrderInfo(receipt, Guid.NewGuid().ToString(), XboxStore.Name);
                Order order = null;

                // ULO-9665 Handle Deferred Orders
                switch (xStoreProduct.ProductKind)
                {
                    // ULO-9666 Handle Store-Managed Consumables
                    // For now, users can use UnmanagedConsumable and let this package do the managing.
                    case XStoreProductKind.Consumable:
                    case XStoreProductKind.UnmanagedConsumable:
                        // This product was purchased but not fulfilled.
                        order = new PendingOrder(cart, info);
                        break;
                    case XStoreProductKind.Durable:
                        foreach (var sku in xStoreProduct.Skus)
                        {
                            // check which sku is entitled. At least one sku in the list should be entitled
                            if (sku.IsInUserCollection)
                            {
                                var data = sku.CollectionData;
                                if (data.EndDate > 0)
                                {
                                    // Subscription
                                    var purchasedProductInfo = new PurchasedProductInfo(xStoreProduct.StoreId, ProductType.Subscription, xStoreProduct);
                                    order = new ConfirmedOrder(cart, info);
                                    // ULO-8118 Move IOrderInfo population to outside of orders,
                                    // so it can be passed into the ConfirmedOrder constructor
                                    order.Info.PurchasedProductInfo = new List<IPurchasedProductInfo> { purchasedProductInfo };
                                    break;
                                }
                                else
                                {
                                    // Non-consumable
                                    order = new ConfirmedOrder(cart, info);
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        order = new ConfirmedOrder(cart, info);
                        break;
                }

                if (order != null)
                {
                    orders.Add(order);
                }
            }

            PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
        }
#endregion

#region Purchase
        public override void Purchase(ICart cart)
        {
            // ULO-9667 CartValidator
            // m_CartValidator.Validate(cart);
            var productDefinition = cart.Items().First().Product.definition;
            m_PurchaseService.ShowPurchaseUIAsync(_storeContext, productDefinition.storeSpecificId, ShowPurchaseCallback);
        }

        private void ShowPurchaseCallback(int hResult, string storeSpecificId)
        {
            var product = FindProduct(storeSpecificId);
            if (HR.FAILED(hResult))
            {
                PurchaseCallback?.OnPurchaseFailed(CreateFailedOrder(product == null ? null : new Cart(product), hResult));
                return;
            }

            // hResult is successful, so respect a successful purchase
            product ??= Product.CreateUnknownProduct(storeSpecificId);

            // ULO-9664 Track Receipts and Transaction IDs
            var info = new OrderInfo("receipt", Guid.NewGuid().ToString(), XboxStore.Name);
            var pendingOrder = new PendingOrder(new Cart(product), info);
            PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
        }
#endregion

#region FinishTransaction
        public override void FinishTransaction(PendingOrder pendingOrder)
        {
            var product = pendingOrder.CartOrdered.Items().First().Product;
            if (!Guid.TryParse(pendingOrder.Info.TransactionID, out var trackingId))
            {
                // Fail fast to force a proper transaction ID
                ConfirmCallback?.OnConfirmOrderFailed(CreateFailedOrder(pendingOrder.CartOrdered, HR.E_INVALIDARG));
            }
            else if (product.definition.type == ProductType.Consumable)
            {
                m_PurchaseService.FulfillConsumableAsync(_storeContext, product, trackingId, FulfillConsumableAsyncCallback);
            }
            else
            {
                // Other types don't need fulfillment
                ConfirmCallback?.OnConfirmOrderSucceeded(trackingId.ToString());
            }
        }

        private void FulfillConsumableAsyncCallback(int hResult, Product product, Guid trackingId, XStoreConsumableResult result)
        {
            if (HR.FAILED(hResult))
            {
                var cart = new Cart(product);
                // ULO-9668 Pass trackingId so fulfillment can be retried
                ConfirmCallback?.OnConfirmOrderFailed(CreateFailedOrder(cart, hResult));
            }
            else
            {
                ConfirmCallback?.OnConfirmOrderSucceeded(trackingId.ToString());
            }
        }
#endregion

#region CheckEntitlement
        public override void CheckEntitlement(ProductDefinition productDefinition)
        {
            m_QueryEntitlementsService.QueryEntitlementsAsync(_storeContext, ConvertToProductKind(productDefinition.type), productDefinition, CheckEntitlementAsyncCallback);
        }

        private void CheckEntitlementAsyncCallback(int hResult, List<XStoreProduct> entitledProducts, ProductDefinition productToCheck)
        {
            if (HR.FAILED(hResult))
            {
                EntitlementCallback?.OnCheckEntitlement(productToCheck, EntitlementStatus.Unknown);
                return;
            }

            var status = EntitlementStatus.NotEntitled;
            foreach (var xStoreProduct in entitledProducts)
            {
                if (xStoreProduct.StoreId == productToCheck.storeSpecificId)
                {
                    status = EntitlementStatus.FullyEntitled;
                    if ((xStoreProduct.ProductKind == XStoreProductKind.Consumable || xStoreProduct.ProductKind == XStoreProductKind.UnmanagedConsumable)
                        && xStoreProduct.IsInUserCollection)
                    {
                        // Consumables are removed from the user collection once fulfilled
                        status = EntitlementStatus.EntitledButNotFinished;
                    }
                    break;
                }
            }
            EntitlementCallback?.OnCheckEntitlement(productToCheck, status);
        }
#endregion

#region Converters
        internal Product FindProduct(string storeSpecificId)
        {
            return ProductCache.GetProducts().FirstOrDefault(product => product.definition.storeSpecificId == storeSpecificId);
        }

        private static Product ConvertToUnknownProduct(XStoreProduct xStoreProduct)
        {
            // This is a little more than Product.CreateUnknownProduct as some properties are known.
            var definition = new ProductDefinition(xStoreProduct.StoreId, xStoreProduct.StoreId, ConvertToProductType(xStoreProduct), true);
            var metadata = ConvertToProductMetadata(xStoreProduct);
            return new Product(definition, metadata);
        }

        private static ProductDescription ConvertToProductDescription(XStoreProduct xStoreProduct)
        {
            return new ProductDescription(xStoreProduct.StoreId, ConvertToProductMetadata(xStoreProduct), null, null, ConvertToProductType(xStoreProduct));
        }

        private static ProductMetadata ConvertToProductMetadata(XStoreProduct xStoreProduct)
        {
            return new ProductMetadata(xStoreProduct.Price.FormattedPrice, xStoreProduct.Title, xStoreProduct.Description, xStoreProduct.Price.CurrencyCode, new Decimal(xStoreProduct.Price.Price));
        }

        private static ProductType ConvertToProductType(XStoreProduct xStoreProduct)
        {
            switch (xStoreProduct.ProductKind)
            {
                case XStoreProductKind.Consumable:
                case XStoreProductKind.UnmanagedConsumable:
                    return ProductType.Consumable;
                case XStoreProductKind.Durable:
                    return IsSubscription(xStoreProduct) ? ProductType.Subscription : ProductType.NonConsumable;
                case XStoreProductKind.Pass:
                    return ProductType.Subscription;
                case XStoreProductKind.None:
                case XStoreProductKind.Game:
                default:
                    return ProductType.Unknown;
            }
        }

        private static bool IsSubscription(XStoreProduct xStoreProduct)
        {
            if (xStoreProduct.ProductKind != XStoreProductKind.Durable)
            {
                return false;
            }

            var sku = xStoreProduct.Skus.FirstOrDefault();
            if (sku == null)
            {
                return false;
            }
            else if (sku.IsSubscription)
            {
                return true;
            }

            var data = GetCollectionDataFromProduct(xStoreProduct);
            // A Subscription is a Durable that expires. If it doesn't expire then it's a non-consumable.
            return data?.EndDate > 0;
        }

        private static XStoreCollectionData GetCollectionDataFromProduct(XStoreProduct xStoreProduct)
        {
            if (xStoreProduct.Skus.Count() == 1)
            {
                return xStoreProduct.Skus.First().CollectionData;
            }

            foreach (var sku in xStoreProduct.Skus)
            {
                var data = sku.CollectionData;
                if (data.Quantity > 0)
                {
                    return data;
                }
            }
            return null;
        }

        internal static XStoreProductKind ConvertToProductKind(ProductType type)
        {
            switch (type)
            {
                case ProductType.Consumable:
                    return XStoreProductKind.UnmanagedConsumable;
                case ProductType.NonConsumable:
                case ProductType.Subscription:
                    return XStoreProductKind.Durable;
            }
            return XStoreProductKind.None;
        }

        internal static PurchasesFetchFailureDescription CreatePurchasesFetchFailureDescription(int hResult)
        {
            var reason = PurchasesFetchFailureReason.Unknown;
            var message = $"Failed to get purchased products. hr=0x{hResult:X}";
            switch (hResult)
            {
                case unchecked((Int32)0x803F6300): // XSTORE_E_NULL_LICENSE_SERVICE_CONTEXT
                case unchecked((Int32)0x803F6301): // XSTORE_E_NULL_STORE_CONTEXT
                    reason = PurchasesFetchFailureReason.PurchasingUnavailable;
                    break;
                case unchecked((Int32)0x801901AD): // HTTP 429 Too Many Requests
                    reason = PurchasesFetchFailureReason.PurchasingUnavailable;
                    message = $"Too many requests, try again later. hr=0x{hResult:X}";
                    break;
                case unchecked((Int32)0x803F6302): // XSTORE_E_INVALID_ID
                    message = $"Invalid user ID. hr=0x{hResult:X}";
                    reason = PurchasesFetchFailureReason.Unknown;
                    break;
            }

            return new PurchasesFetchFailureDescription(reason, message);
        }

        internal static FailedOrder CreateFailedOrder(ICart cart, int hResult)
        {
            if (cart == null)
            {
                return new FailedOrder(new Cart(Product.CreateUnknownProduct("InvalidProduct")), PurchaseFailureReason.ProductUnavailable, "Product is not available for purchase");
            }

            var reason = PurchaseFailureReason.Unknown;
            string details;
            switch (hResult)
            {
                case HR.E_ABORT:
                    reason = PurchaseFailureReason.UserCancelled;
                    details = $"User cancelled purchase. hr=0x{hResult:X}";
                    break;
                case HR.E_INVALIDARG:
                    reason = PurchaseFailureReason.Unknown;
                    details = $"Invalid transaction ID. Xbox Store requires transaction IDs to be GUIDs.  hr=0x{hResult:X}";
                    break;
                case unchecked((Int32)0x89245304): // E_GAMESTORE_ALREADY_PURCHASED
                    var product = cart.Items().First()?.Product;
                    if (product != null && product.definition.type == ProductType.Consumable)
                    {
                        reason = PurchaseFailureReason.ExistingPurchasePending;
                        details = $"An existing purchase hasn't been consumed yet. hr=0x{hResult:X}";
                    }
                    else
                    {
                        reason = PurchaseFailureReason.DuplicateTransaction;
                        details = $"Product is already purchased. hr=0x{hResult:X}";
                    }
                    break;
                // ULO-9669 Find more failure scenarios that have different error codes
                // So far, every kind of error seems to return E_ABORT, see if there's a way to get the real reason.
                case HR.E_FAIL:
                default:
                    details = $"Purchase failed. hr=0x{hResult:X}";
                    break;
            }

            return new FailedOrder(cart, reason, details);
        }
#endregion
    }
}
#endif
