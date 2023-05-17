#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The main controller for Applications using Unity Purchasing.
    /// </summary>
    internal class PurchasingManager : IStoreCallback, IStoreController
    {
        private readonly IStore m_Store;
        private IInternalStoreListener? m_Listener;
        private readonly ILogger m_Logger;
        private readonly TransactionLog m_TransactionLog;
        private readonly string m_StoreName;
        private readonly IUnityServicesInitializationChecker m_UnityServicesInitializationChecker;
        private Action? m_AdditionalProductsCallback;
        private Action<InitializationFailureReason>? m_AdditionalProductsFailCallback;
        private Action<InitializationFailureReason, string?>? m_AdditionalProductsDetailedFailCallback;

        private readonly HashSet<string> purchasesProcessedInSession = new HashSet<string>();

        /// <summary>
        /// Stores may opt to disable Unity IAP's transaction log.
        /// </summary>
        public bool useTransactionLog { get; set; }

        internal PurchasingManager(TransactionLog tDb, ILogger logger, IStore store, string storeName, IUnityServicesInitializationChecker unityServicesInitializationChecker)
        {
            m_TransactionLog = tDb;
            m_Store = store;
            m_Logger = logger;
            m_StoreName = storeName;
            useTransactionLog = true;
            m_UnityServicesInitializationChecker = unityServicesInitializationChecker;
        }

        public void InitiatePurchase(Product product)
        {
            InitiatePurchase(product, string.Empty);
        }

        public void InitiatePurchase(string? productId)
        {
            InitiatePurchase(productId, string.Empty);
        }

        public void InitiatePurchase(Product? product, string developerPayload)
        {
            m_UnityServicesInitializationChecker.CheckAndLogWarning();

            if (null == product)
            {
                m_Logger.LogIAPWarning("Trying to purchase null Product");
                return;
            }

            if (!product.availableToPurchase)
            {
                m_Listener?.OnPurchaseFailed(product, new PurchaseFailureDescription(product.transactionID, PurchaseFailureReason.ProductUnavailable,
                    "No products were found when fetching from the store"));
                return;
            }

            m_Store.Purchase(product.definition, developerPayload);
        }

        public void InitiatePurchase(string? purchasableId, string developerPayload)
        {
            var product = products.WithID(purchasableId);
            if (null == product)
            {
                m_Logger.LogFormat(LogType.Warning, "Unable to purchase unknown product with id: {0}", purchasableId);
            }

            InitiatePurchase(product, developerPayload);
        }

        /// <summary>
        /// Where an Application returned ProcessingResult.Pending they can manually
        /// finish the transaction by calling this method.
        /// </summary>
        public void ConfirmPendingPurchase(Product product)
        {
            if (null == product)
            {
                m_Logger.LogIAPError("Unable to confirm purchase with null Product");
                return;
            }

            if (string.IsNullOrEmpty(product.transactionID))
            {
                m_Logger.LogIAPError("Unable to confirm purchase; Product has missing or empty transactionID");
                return;
            }

            if (useTransactionLog)
            {
                m_TransactionLog.Record(product.transactionID);
            }

            m_Store.FinishTransaction(product.definition, product.transactionID);
            m_Listener?.SendTransactionEvent(product);
        }

        public ProductCollection products { get; private set; } = null!;

        /// <summary>
        /// Called by our IStore when a purchase succeeds.
        /// </summary>
        public void OnPurchaseSucceeded(string id, string? receipt, string transactionId)
        {
            var product = products.WithStoreSpecificID(id);
            if (null == product)
            {
                // If is possible for stores to tell us about products we have not yet
                // requested details of.
                // We should still tell the App in this scenario, albeit with incomplete information.
                var definition = new ProductDefinition(id, ProductType.NonConsumable);
                product = new Product(definition, new ProductMetadata());
            }

            UpdateProductReceiptAndTransactionID(product, receipt, transactionId);
            ProcessPurchaseIfNew(product);
        }

        void UpdateProductReceiptAndTransactionID(Product product, string? receipt, string transactionId)
        {
            if (product != null)
            {
                product.receipt = CreateUnifiedReceipt(receipt, transactionId);
                product.transactionID = transactionId;
            }
        }

        public void OnAllPurchasesRetrieved(List<Product> purchasedProducts)
        {
            if (products != null)
            {
                foreach (var product in products.all)
                {
                    var purchasedProduct = purchasedProducts?.FirstOrDefault(firstPurchasedProduct => firstPurchasedProduct.definition.id == product.definition.id);
                    if (purchasedProduct != null)
                    {
                        HandlePurchaseRetrieved(product, purchasedProduct);
                    }
                    else
                    {
                        ClearProductReceipt(product);
                    }
                }
            }
        }

        // TODO IAP-2929: Add this to IStoreCallback in a major release
        internal static void OnEntitlementRevoked(Product revokedProduct)
        {
            ClearProductReceipt(revokedProduct);
        }

        void HandlePurchaseRetrieved(Product product, Product purchasedProduct)
        {
            UpdateProductReceiptAndTransactionID(product, purchasedProduct.receipt, purchasedProduct.transactionID);
            if (initialized && !WasPurchaseAlreadyProcessed(purchasedProduct.transactionID))
            {
                ProcessPurchaseIfNew(product);
            }
        }

        bool WasPurchaseAlreadyProcessed(string transactionId)
        {
            return purchasesProcessedInSession.Contains(transactionId);
        }

        static void ClearProductReceipt(Product product)
        {
            product.receipt = null;
            product.transactionID = null;
        }

        [Obsolete]
        public void OnSetupFailed(InitializationFailureReason reason)
        {
            OnSetupFailed(reason, null);
        }

        public void OnSetupFailed(InitializationFailureReason reason, string? message)
        {
            if (initialized)
            {
                m_AdditionalProductsFailCallback?.Invoke(reason);
                m_AdditionalProductsDetailedFailCallback?.Invoke(reason, message);
            }
            else
            {
                m_Listener?.OnInitializeFailed(reason, message);
            }
        }

        public void OnPurchaseFailed(PurchaseFailureDescription description)
        {
            if (description != null)
            {
                var product = products.WithStoreSpecificID(description.productId);
                if (null == product)
                {
                    m_Logger.LogFormat(LogType.Error, "Failed to purchase unknown product {0}", "productId:" + description.productId + " reason:" + description.reason + " message:" + description.message);
                    return;
                }

                m_Logger.LogFormat(LogType.Warning, "onPurchaseFailedEvent({0})", "productId:" + product.definition.id + " message:" + description.message);
                m_Listener?.OnPurchaseFailed(product, description);
            }
        }

        /// <summary>
        /// Called back by our IStore when it has fetched the latest product data.
        /// </summary>
        public void OnProductsRetrieved(List<ProductDescription> products)
        {
            var unknownProducts = new HashSet<Product>();
            foreach (var product in products)
            {
                var matchedProduct = this.products.WithStoreSpecificID(product.storeSpecificId);
                if (null == matchedProduct)
                {
                    var definition = new ProductDefinition(product.storeSpecificId,
                        product.storeSpecificId, product.type);
                    matchedProduct = new Product(definition, product.metadata);
                    unknownProducts.Add(matchedProduct);
                }

                matchedProduct.availableToPurchase = true;
                matchedProduct.metadata = product.metadata;
                matchedProduct.transactionID = product.transactionId;

                if (!string.IsNullOrEmpty(product.receipt))
                {
                    matchedProduct.receipt = CreateUnifiedReceipt(product.receipt, product.transactionId);
                }
            }

            if (unknownProducts.Count > 0)
            {
                this.products.AddProducts(unknownProducts);
            }

            // Fire our initialisation events if this is a first poll.
            CheckForInitialization();

            ProcessPurchaseOnStart();
        }

        string CreateUnifiedReceipt(string? rawReceipt, string transactionId)
        {
            return UnifiedReceiptFormatter.FormatUnifiedReceipt(rawReceipt, transactionId, m_StoreName);
        }

        void ProcessPurchaseOnStart()
        {
            foreach (var product in products.set)
            {
                if (!string.IsNullOrEmpty(product.receipt) && !string.IsNullOrEmpty(product.transactionID))
                {
                    ProcessPurchaseIfNew(product);
                }
            }
        }

        [Obsolete]
        public void FetchAdditionalProducts(HashSet<ProductDefinition> additionalProducts, Action successCallback,
            Action<InitializationFailureReason> failCallback)
        {
            m_AdditionalProductsCallback = successCallback;
            m_AdditionalProductsFailCallback = failCallback;
            products.AddProducts(additionalProducts.Select(x => new Product(x, new ProductMetadata())));
            m_Store.RetrieveProducts(new ReadOnlyCollection<ProductDefinition>(additionalProducts.ToList()));
        }

        public void FetchAdditionalProducts(HashSet<ProductDefinition> additionalProducts, Action successCallback, Action<InitializationFailureReason, string?> failCallback)
        {
            m_AdditionalProductsCallback = successCallback;
            m_AdditionalProductsDetailedFailCallback = failCallback;
            products.AddProducts(additionalProducts.Select(x => new Product(x, new ProductMetadata())));
            m_Store.RetrieveProducts(new ReadOnlyCollection<ProductDefinition>(additionalProducts.ToList()));
        }

        /// <summary>
        /// Checks the product's transaction ID for uniqueness
        /// against the transaction log and calls the Application's
        /// ProcessPurchase method if so.
        /// </summary>
        private void ProcessPurchaseIfNew(Product product)
        {
            if (HasRecordedTransaction(product.transactionID))
            {
                m_Store.FinishTransaction(product.definition, product.transactionID);
                return;
            }

            purchasesProcessedInSession.Add(product.transactionID);

            var p = new PurchaseEventArgs(product);

            // Applications may elect to delay confirmations of purchases,
            // such as when persisting purchase state asynchronously.
            if (m_Listener?.ProcessPurchase(p) == PurchaseProcessingResult.Complete)
            {
                ConfirmPendingPurchase(product);
            }
        }

        bool HasRecordedTransaction(string transactionId)
        {
            return useTransactionLog && m_TransactionLog.HasRecordOf(transactionId);
        }

        private bool initialized;

        private void CheckForInitialization()
        {
            if (!initialized)
            {
                initialized = true;

                var hasAvailableProductsToPurchase = HasAvailableProductsToPurchase();

                if (hasAvailableProductsToPurchase)
                {
                    m_Listener?.OnInitialized(this);
                }
                else
                {
                    m_Listener?.OnInitializeFailed(InitializationFailureReason.NoProductsAvailable);
                }
            }
            else
            {
                m_AdditionalProductsCallback?.Invoke();
            }
        }

        bool HasAvailableProductsToPurchase(bool shouldLogUnavailableProducts = true)
        {
            var available = false;
            foreach (var product in products.set)
            {
                if (product.availableToPurchase)
                {
                    available = true;
                }
                else if (shouldLogUnavailableProducts)
                {
                    m_Logger.LogFormat(LogType.Warning, "Unavailable product {0}-{1}", product.definition.id, product.definition.storeSpecificId);
                }
            }

            return available;
        }

        public void Initialize(IInternalStoreListener listener, HashSet<ProductDefinition> products)
        {
            m_Listener = listener;
            m_Store.Initialize(this);

            var prods = products.Select(x => new Product(x, new ProductMetadata())).ToArray();
            this.products = new ProductCollection(prods);

            var productCollection = new ReadOnlyCollection<ProductDefinition>(products.ToList());

            // Start the initialisation process by fetching product metadata.
            m_Store.RetrieveProducts(productCollection);
        }
    }
}
