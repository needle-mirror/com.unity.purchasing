using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The main controller for Applications using Unity Purchasing.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    class PurchasingManager : IStoreController
    {
        /// <summary>
        /// Stores may opt to disable Unity IAP's transaction log.
        /// </summary>
        public bool useTransactionLog { get; set; }

        readonly ITransactionLog m_TransactionLog = new TransactionLog(Application.persistentDataPath);

        StoreController m_StoreController = new();
        public PurchasingManager()
        {
            m_StoreController.OnPurchaseFailed += OnPurchaseFailedAction;
            m_StoreController.OnPurchasePending += OnPurchasePendingAction;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmedAction;
            m_StoreController.OnPurchasesFetched += OnPurchasesFetchedAction;

            var defaultStore = DefaultStoreHelper.GetDefaultBuiltInAppStore();
            if (defaultStore is AppStore.AppleAppStore or AppStore.MacAppStore)
            {
                UnityIAPServices.DefaultPurchase().Apple?.SetRefreshAppReceipt(true);
            }
            useTransactionLog = true;
        }

        public ProductCollection products { get; } = new();

        public void InitiatePurchase(Product product, string payload)
        {
            InitiatePurchase(product.definition.id);
        }

        public void InitiatePurchase(string productId, string payload)
        {
            InitiatePurchase(productId);
        }

        public void InitiatePurchase(Product product)
        {
            InitiatePurchase(product.definition.id);
        }

        public void InitiatePurchase(string productId)
        {
            m_StoreController.Purchase(CreateCart(productId));
        }

        static void OnPurchaseFailedAction(FailedOrder failedOrder)
        {
            UnityPurchasing.m_StoreListener?.OnPurchaseFailed(failedOrder.CartOrdered.Items().FirstOrDefault()?.Product, failedOrder.FailureReason);
        }

        void OnPurchasePendingAction(PendingOrder pendingOrder)
        {
            var product = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null)
            {
                product.transactionID = pendingOrder.Info.TransactionID;
                product.receipt = pendingOrder.Info.Receipt;
                product.appleOriginalTransactionID = pendingOrder.Info.Apple?.OriginalTransactionID;
                var localProduct = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == product.definition.id);
                if (localProduct != null)
                {
                    localProduct.transactionID = pendingOrder.Info.TransactionID;
                    localProduct.receipt = pendingOrder.Info.Receipt;
                    localProduct.appleOriginalTransactionID = pendingOrder.Info.Apple?.OriginalTransactionID;
                }

                InvokeProcessPurchase(product, pendingOrder);
            }
        }

        void OnPurchaseConfirmedAction(Order order)
        {
            if (order is not ConfirmedOrder confirmedOrder)
            {
                return;
            }

            var product = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product != null && product.definition.type == ProductType.Consumable)
            {
                product.receipt = null;
                product.transactionID = null;
                product.appleOriginalTransactionID = null;
                var localProduct = m_StoreController.GetProducts()
                    .FirstOrDefault(p => p.definition.id == product.definition.id);
                if (localProduct != null)
                {
                    localProduct.receipt = null;
                    localProduct.transactionID = null;
                    localProduct.appleOriginalTransactionID = null;
                }
            }
        }

        void OnPurchasesFetchedAction(Orders orders)
        {
            foreach (var order in orders.PendingOrders)
            {
                foreach (var cartItem in order.CartOrdered.Items())
                {
                    var product = cartItem.Product;
                    product.receipt = order.Info.Receipt;
                    product.transactionID = order.Info.TransactionID;
                    product.appleOriginalTransactionID = order.Info.Apple?.OriginalTransactionID;
                    var localProduct = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == product.definition.id);
                    if (localProduct != null)
                    {
                        localProduct.receipt = order.Info.Receipt;
                        localProduct.transactionID = order.Info.TransactionID;
                        localProduct.appleOriginalTransactionID = order.Info.Apple?.OriginalTransactionID;
                    }
                }
            }

            foreach (var order in orders.ConfirmedOrders)
            {
                foreach (var cartItem in order.CartOrdered.Items())
                {
                    var product = cartItem.Product;
                    product.receipt = order.Info.Receipt;
                    product.transactionID = order.Info.TransactionID;
                    product.appleOriginalTransactionID = order.Info.Apple?.OriginalTransactionID;
                    var localProduct = m_StoreController.GetProducts().FirstOrDefault(p => p.definition.id == product.definition.id);
                    if (localProduct != null)
                    {
                        localProduct.receipt = order.Info.Receipt;
                        localProduct.transactionID = order.Info.TransactionID;
                        localProduct.appleOriginalTransactionID = order.Info.Apple?.OriginalTransactionID;
                    }

                    InvokeProcessPurchase(product, order);
                }
            }
        }

        void InvokeProcessPurchase(Product product, Order order)
        {
            if (order is ConfirmedOrder && HasRecordedTransaction(product.transactionID))
            {
                return;
            }

            var processPurchaseResult = UnityPurchasing.m_StoreListener?.ProcessPurchase(new PurchaseEventArgs(product));
            if (processPurchaseResult == PurchaseProcessingResult.Complete)
            {
                if (order is PendingOrder)
                {
                    ConfirmPendingPurchase(product);
                }
                else if (useTransactionLog)
                {
                    m_TransactionLog.Record(product.transactionID);
                }
            }
        }

        ICart CreateCart(string productId)
        {
            var product = FindProductByProductId(productId);
            var cartItem = new CartItem(product);
            return new Cart(cartItem);
        }

        Product FindProductByProductId(string productId)
        {
            return m_StoreController.GetProducts().FirstOrDefault(product => product.definition.id == productId);
        }

        public void FetchAdditionalProducts(HashSet<ProductDefinition> additionalProducts, Action successCallback, Action<InitializationFailureReason, string> failCallback)
        {
            Action<List<Product>> onFetched = null;
            Action<ProductFetchFailed> onFailed = null;

            onFetched = _ =>
            {
                m_StoreController.OnProductsFetched -= onFetched;
                m_StoreController.OnProductsFetchFailed -= onFailed;
                successCallback();
            };

            onFailed = failed =>
            {
                m_StoreController.OnProductsFetched -= onFetched;
                m_StoreController.OnProductsFetchFailed -= onFailed;
                failCallback(InitializationFailureReason.NoProductsAvailable, failed.FailureReason);
            };

            m_StoreController.OnProductsFetched += onFetched;
            m_StoreController.OnProductsFetchFailed += onFailed;

            m_StoreController.FetchProductsWithNoRetries(new List<ProductDefinition>(additionalProducts));
        }

        public void ConfirmPendingPurchase(Product product)
        {
            m_StoreController.ConfirmPurchase(CreatePendingOrderFromProduct(product));

            if (useTransactionLog)
            {
                m_TransactionLog.Record(product.transactionID);
            }
        }

        static PendingOrder CreatePendingOrderFromProduct(Product product)
        {
            foreach (var order in PurchaseServiceProvider.GetDefaultPurchaseService().GetPurchases())
            {
                if (order is PendingOrder pendingOrder)
                {
                    var cartItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
                    if (cartItem != null && cartItem.Product.definition.storeSpecificId == product.definition.storeSpecificId)
                    {
                        return pendingOrder; // Return the original instance
                    }
                }
            }
            Debug.LogWarning($"No pending order found for product {product.definition.id}. Returning a new PendingOrder with empty OrderInfo.");
            var cartItemNew = new CartItem(product);
            var cartNew = new Cart(cartItemNew);
            return new PendingOrder(cartNew, new OrderInfo(string.Empty, string.Empty, string.Empty));
        }

        bool HasRecordedTransaction(string transactionId)
        {
            return useTransactionLog && m_TransactionLog.HasRecordOf(transactionId);
        }
    }
}
