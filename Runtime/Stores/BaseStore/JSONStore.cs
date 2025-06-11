#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Purchasing.Extension;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Internal store implementation passing store requests from the user through to the underlaying
    /// native store system, and back again. Binds a native store system binding to a callback.
    /// </summary>
    class JsonStore : InternalStore, IUnityCallback
    {
        protected ICartValidator m_CartValidator;
        protected string m_StoreName;

        INativeStore? m_Store;
        bool m_IsRefreshing;

        Action? m_RefreshCallback;

        protected readonly ILogger Logger;

        // ITransactionHistoryExtensions stuff
        //
        // Enhanced error information
        PurchaseFailureDescription? LastPurchaseFailureDescription;
        StoreSpecificPurchaseErrorCode m_LastPurchaseErrorCode = StoreSpecificPurchaseErrorCode.Unknown;

        const string k_StoreSpecificErrorCodeKey = "storeSpecificErrorCode";

        /// <summary>
        /// No arg constructor due to cyclical dependency on IUnityCallback.
        /// </summary>
        public void SetNativeStore(INativeStore native)
        {
            m_Store = native;
        }

        internal JsonStore(ICartValidator cartValidator, ILogger logger, string storeName)
        {
            m_CartValidator = cartValidator;
            Logger = logger;
            m_StoreName = storeName;
        }

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_Store?.FetchProducts(JSONSerializer.SerializeProductDefs(products));
        }

        internal void ProcessManagedStoreResponse(List<ProductDefinition>? storeProducts)
        {
            if (m_IsRefreshing)
            {
                m_IsRefreshing = false;

                // Skip native store layer during refresh if catalog contains no information
                if (storeProducts?.Count == 0 && m_RefreshCallback != null)
                {
                    m_RefreshCallback();
                    m_RefreshCallback = null;
                    return;
                }
            }

            var products = new HashSet<ProductDefinition>();
            if (storeProducts != null)
            {
                products.UnionWith(storeProducts);
            }

            m_Store?.FetchProducts(JSONSerializer.SerializeProductDefs(products));
        }

        public override void FetchPurchases()
        {
            m_Store?.FetchExistingPurchases();
        }

        public void OnProductsFetchFailed(string jsonFailureDescription)
        {
            var description = JSONSerializer.DeserializeProductFetchFailureDescription(jsonFailureDescription);
            ProductsCallback?.OnProductsFetchFailed(description);
        }

        public void OnPurchasesRetrievalFailed(string jsonFailureDescription)
        {
            var description = JSONSerializer.DeserializePurchasesFetchFailureDescription(jsonFailureDescription);
            PurchaseFetchCallback?.OnPurchasesRetrievalFailed(description);
        }

        public virtual void OnPurchasesFetched(string json)
        {
            var productDescriptions = JSONSerializer.DeserializeProductDescriptions(json);
            var orders = CreateOrdersFromFetchedPurchases(productDescriptions);
            PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
        }

        internal List<Order> CreateOrdersFromFetchedPurchases(List<ProductDescription> productDescriptions)
        {
            var orders = new List<Order>();
            foreach (var product in productDescriptions)
            {
                if (product.type == ProductType.Consumable)
                {
                    orders.Add(GeneratePendingOrder(product.storeSpecificId, product.receipt, product.transactionId));
                }
                else
                {
                    orders.Add(GenerateConfirmedOrder(product.storeSpecificId, product.receipt, product.transactionId));
                }
            }

            return orders;
        }

        public override void Purchase(ICart cart)
        {
            m_CartValidator.Validate(cart);
            var productDefinition = cart.Items().First().Product.definition;
            Purchase(productDefinition, string.Empty);
        }

        protected virtual void Purchase(ProductDefinition productDefinition, string developerPayload)
        {
            m_Store?.Purchase(JSONSerializer.SerializeProductDef(productDefinition), developerPayload);
        }

        public override void FinishTransaction(PendingOrder pendingOrder)
        {
            m_CartValidator.Validate(pendingOrder.CartOrdered);
            var productDefinition = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition;
            FinishTransaction(productDefinition, pendingOrder.Info.TransactionID);
            ConfirmCallback?.OnConfirmOrderSucceeded(pendingOrder.Info.TransactionID);
        }

        protected virtual void FinishTransaction(ProductDefinition? productDefinition, string transactionId)
        {
            // Product definitions may be null if a store tells Unity IAP about an unknown productDefinition;
            // Unity IAP will not have a corresponding definition but will still finish the transaction.
            var def = productDefinition == null ? null : JSONSerializer.SerializeProductDef(productDefinition);
            m_Store?.FinishTransaction(def, transactionId);
        }

        public override void Connect()
        {
            m_Store?.Connect();
        }

        public void OnStoreConnectionSucceeded()
        {
            ConnectCallback?.OnStoreConnectionSucceeded();
        }

        public void OnStoreConnectionFailed(string jsonFailureDescription)
        {
            var failureDescription = JSONSerializer.DeserializeConnectionFailureDescription(jsonFailureDescription);
            ConnectCallback?.OnStoreConnectionFailed(failureDescription);
        }

        public override void CheckEntitlement(ProductDefinition product)
        {
            m_Store?.CheckEntitlement(JSONSerializer.SerializeProductDef(product));
        }

        public virtual void OnProductsFetched(string json)
        {
            // NB: AppleStoreImpl overrides this completely and does not call the base.
            var productDescriptions = JSONSerializer.DeserializeProductDescriptions(json);

            ProductsCallback?.OnProductsFetched(productDescriptions);
        }

        public virtual void OnPurchaseSucceeded(string id, string receipt, string transactionID)
        {
            var order = GeneratePendingOrder(id, receipt, transactionID);
            PurchaseCallback?.OnPurchaseSucceeded(order);
        }

        protected DeferredOrder GenerateDeferredOrder(string id, string receipt, string transactionID)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new DeferredOrder(cart, new OrderInfo(receipt, transactionID, m_StoreName));
        }

        protected PendingOrder GeneratePendingOrder(string id, string receipt, string transactionID)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new PendingOrder(cart, new OrderInfo(receipt, transactionID, m_StoreName));
        }

        protected ConfirmedOrder GenerateConfirmedOrder(string id, string receipt, string transactionID)
        {
            var product = FindProductById(id);
            var cart = new Cart(product);
            return new ConfirmedOrder(cart, new OrderInfo(receipt, transactionID, m_StoreName));
        }

        protected Product FindProductById(string productId)
        {
            return ProductCache.FindOrDefault(productId);
        }

        public void OnPurchaseFailed(string json)
        {
            try
            {
                var purchaseDetails = JSONSerializer.DeserializePurchaseDetails(json);

                var productId = purchaseDetails.TryGetString("productId");
                var verificationError = purchaseDetails.TryGetString("verificationError");
                PurchaseFailureReason reason = (PurchaseFailureReason)Convert.ToInt32(purchaseDetails.TryGetString("reason"));

                var description = new PurchaseFailureDescription(ProductCache.FindOrDefault(productId), reason, verificationError);
                OnPurchaseFailed(description);
            }
            catch
            {
                OnPurchaseFailed(new PurchaseFailureDescription(Product.CreateUnknownProduct("Unknown ProductID"), PurchaseFailureReason.Unknown, "Unable to parse purchase failure details"));
            }
        }

        public void OnPurchaseFailed(PurchaseFailureDescription failure, string? json = null)
        {
            LastPurchaseFailureDescription = failure;
            m_LastPurchaseErrorCode = ParseStoreSpecificPurchaseErrorCode(json);

            PurchaseCallback?.OnPurchaseFailed(failure.ConvertToFailedOrder());
        }

        public virtual void OnPurchaseDeferred(string productDetails)
        {
            // NB: AppleStoreImpl overrides this completely and does not call the base.
            var productDescriptions = JSONSerializer.DeserializeProductDescriptions(productDetails);
            var productDescription = productDescriptions.FirstOrDefault();
            if (productDescription != null)
            {
                var deferredOrder = GenerateDeferredOrder(productDescription.storeSpecificId, productDescription.receipt, productDescription.transactionId);
                PurchaseCallback?.OnPurchaseDeferred(deferredOrder);
            }
        }

        public PurchaseFailureDescription? GetLastPurchaseFailureDescription()
        {
            return LastPurchaseFailureDescription;
        }

        public StoreSpecificPurchaseErrorCode GetLastStoreSpecificPurchaseErrorCode()
        {
            return m_LastPurchaseErrorCode;
        }

        static StoreSpecificPurchaseErrorCode ParseStoreSpecificPurchaseErrorCode(string? json)
        {
            // If we didn't get any JSON just return Unknown.
            if (json == null)
            {
                return StoreSpecificPurchaseErrorCode.Unknown;
            }

            // If the dictionary contains a storeSpecificErrorCode, return it, otherwise return Unknown.
            var purchaseFailureDictionary = MiniJson.JsonDecode(json) as Dictionary<string, object>;
            if (purchaseFailureDictionary != null && purchaseFailureDictionary.ContainsKey(k_StoreSpecificErrorCodeKey) && Enum.IsDefined(typeof(StoreSpecificPurchaseErrorCode), (string)purchaseFailureDictionary[k_StoreSpecificErrorCodeKey]))
            {
                var storeSpecificErrorCodeString = (string)purchaseFailureDictionary[k_StoreSpecificErrorCodeKey];
                return (StoreSpecificPurchaseErrorCode)Enum.Parse(typeof(StoreSpecificPurchaseErrorCode),
                    storeSpecificErrorCodeString);
            }

            return StoreSpecificPurchaseErrorCode.Unknown;
        }

        /// <summary>
        /// Checks the connection state to the store. Not Implemented.
        /// </summary>
        /// <returns>An object describing the connection state.</returns>
        /// <exception cref="NotImplementedException">Not Implemented for this store.</exception>
        public ConnectionState CheckConnectionState()
        {
            throw new NotImplementedException();
        }
    }
}
