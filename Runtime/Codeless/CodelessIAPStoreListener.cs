#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Initializes Unity IAP with the <typeparamref name="Product"/>s defined in the default IAP <see cref="ProductCatalog"/>.
    /// Automatically initializes at runtime load when enabled in the GUI. <see cref="ProductCatalog.enableCodelessAutoInitialization"/>
    /// Manages <typeparamref name="CodelessIAPButton"/>s and <typeparamref name="IAPListener"/>s.
    /// </summary>
    public class CodelessIAPStoreListener
    {
        static CodelessIAPStoreListener? s_Instance;
        readonly List<CodelessIAPButton> m_ActiveCodelessButtons = new List<CodelessIAPButton>();
        readonly List<IAPListener> m_ActiveListeners = new List<IAPListener>();
        bool m_UnityPurchasingInitialized;

        IStoreService? m_StoreService;
        IProductService? m_ProductService;
        IPurchaseService? m_PurchasingService;
        readonly ProductCatalog m_Catalog = ProductCatalog.LoadDefaultCatalog();
        CatalogProvider? m_CatalogProvider;


        /// <summary>
        /// For advanced scripted IAP actions, use this session's <typeparamref name="IStoreController"/> after initialization.
        /// </summary>
        /// <seealso cref="StoreController"/>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        protected IStoreController controller = new PurchasingManager();

        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IExtensionProvider"/> after initialization.
        /// </summary>
        /// <seealso cref="ExtensionProvider"/>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        protected IExtensionProvider extensions = null!;

        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        ConfigurationBuilder m_Builder = null!;

        bool m_InitializationComplete;

        /// <summary>
        /// Allows outside sources to know whether the successful initialization has completed.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public static bool initializationComplete;

        [RuntimeInitializeOnLoadMethod]
        static async void InitializeCodelessPurchasingOnLoad()
        {
            var productCatalog = ProductCatalog.LoadDefaultCatalog();
            if (productCatalog.enableCodelessAutoInitialization && !productCatalog.IsEmpty() && s_Instance == null)
            {
                await CreateCodelessIAPStoreListenerInstance();
            }
        }


        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IStoreConfiguration"/>s.
        /// Note, these instances are only available after initialization through Codeless IAP, currently.
        /// </summary>
        /// <typeparam name="T">A subclass of <typeparamref name="IStoreConfiguration"/> such as <typeparamref name="IAppleConfiguration"/></typeparam>
        /// <returns>Returns the store configuration.</returns>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public T GetStoreConfiguration<T>() where T : IStoreConfiguration
        {
            return m_Builder.Configure<T>();
        }

        /// <summary>
        /// For advanced scripted store-specific IAP actions, use this session's <typeparamref name="IStoreExtension"/>s after initialization.
        /// </summary>
        /// <typeparam name="T">A subclass of <typeparamref name="IStoreExtension"/> such as <typeparamref name="IAppleExtensions"/></typeparam>
        /// <returns>Returns the store extension.</returns>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public T GetStoreExtensions<T>() where T : IStoreExtension
        {
            return extensions.GetExtension<T>();
        }

        /// <summary>
        /// Singleton of me. Initialized on first access.
        /// Also initialized by RuntimeInitializeOnLoadMethod if <typeparamref name="ProductCatalog.enableCodelessAutoInitialization"/>
        /// is true.
        /// </summary>
        public static CodelessIAPStoreListener Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    CreateCodelessIAPStoreListenerInstance().GetAwaiter();
                }
                return s_Instance!;
            }
        }

        /// <summary>
        /// Creates the static instance of CodelessIAPStoreListener and initializes purchasing
        /// </summary>
        static async Task CreateCodelessIAPStoreListenerInstance()
        {
            s_Instance = new CodelessIAPStoreListener();
            if (!s_Instance.m_UnityPurchasingInitialized)
            {
                await s_Instance.AutoInitializeUnityGamingServicesIfEnabled();
                await s_Instance.InitializePurchasing();
            }
        }

        async Task InitializePurchasing()
        {
            m_Builder = new ConfigurationBuilder();
            extensions = new ExtensionProvider();
            CreateServices();
            InitCatalog();

            await ConnectToStore();

            m_UnityPurchasingInitialized = true;
        }

        void InitCatalog()
        {
            m_CatalogProvider = CodelessCatalogProvider.PopulateCatalogProvider(m_Catalog);
        }

        void CreateServices()
        {
            m_StoreService = StoreServiceProvider.GetDefaultStoreService();
            m_ProductService = ProductServiceProvider.GetDefaultProductService();
            m_PurchasingService = PurchaseServiceProvider.GetDefaultPurchaseService();

            ConfigureServiceCallbacks();
        }

        void ConfigureServiceCallbacks()
        {
            ConfigureProductServiceCallbacks();
            ConfigurePurchasingServiceCallbacks();
        }

        void ConfigureProductServiceCallbacks()
        {
            if (m_ProductService != null)
            {
                m_ProductService.OnProductsFetched += OnInitialProductsFetched;
                m_ProductService.OnProductsFetchFailed += OnInitialProductsFetchFailed;
            }
        }

        void ChangeProductServiceCallbacks()
        {
            if (m_ProductService != null)
            {
                m_ProductService.OnProductsFetched -= OnInitialProductsFetched;
                m_ProductService.OnProductsFetchFailed -= OnInitialProductsFetchFailed;

                m_ProductService.OnProductsFetched += OnAdditionalProductsFetched;
                m_ProductService.OnProductsFetchFailed += OnAdditionalProductsFetchFailed;
            }
        }

        void ConfigurePurchasingServiceCallbacks()
        {
            if (m_PurchasingService != null)
            {
                m_PurchasingService.OnPurchasesFetched += OnPurchasesFetched;
                m_PurchasingService.OnPurchasesFetchFailed += OnPurchasesFetchFailure;
                m_PurchasingService.OnPurchasePending += OnOrderPending;
                m_PurchasingService.OnPurchaseConfirmed += OnPurchaseConfirmed;
                m_PurchasingService.OnPurchaseFailed += OnPurchaseFailed;
                m_PurchasingService.OnPurchaseDeferred += OnOrderDeferred;
            }
        }

        void OnInitialProductsFetched(List<Product> products)
        {
            FetchExistingPurchases();

            ChangeProductServiceCallbacks();
            m_InitializationComplete = true;
            initializationComplete = true;
            HandleOnInitForAllButtons();
            InvokeOnProductsFetched(products);
        }


        void HandleOnInitForAllButtons()
        {
            foreach (var button in m_ActiveCodelessButtons)
            {
                button.OnInitCompleted();
            }
        }

        void FetchExistingPurchases()
        {
            m_PurchasingService?.FetchPurchases();
        }

        void InvokeOnProductsFetched(List<Product> products)
        {
            InvokeListenersOnProductsFetched(products);
            InvokeButtonsOnProductsFetched(products);
        }

        void InvokeListenersOnProductsFetched(List<Product> products)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnProductsFetched(products);
            }
        }

        void InvokeButtonsOnProductsFetched(List<Product> products)
        {
            foreach (var product in products)
            {
                foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == product.definition.id))
                {
                    button.OnProductFetched(product);
                }
            }
        }

        void OnInitialProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            InvokeListenersOnProductsFetchFailed(productFetchFailed);
            InvokeButtonsOnProductsFetchFailed(productFetchFailed);
        }

        void InvokeListenersOnProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnProductsFetchFailed(productFetchFailed);
            }
        }

        void InvokeButtonsOnProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            foreach (var product in productFetchFailed.FailedFetchProducts)
            {
                foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == product.id))
                {
                    button.OnProductFetchFailed(product, productFetchFailed.FailureReason);
                }
            }
        }

        void OnAdditionalProductsFetched(List<Product> products)
        {
            InvokeOnProductsFetched(products);
        }

        void OnAdditionalProductsFetchFailed(ProductFetchFailed productFetchFailed)
        {
            InvokeListenersOnProductsFetchFailed(productFetchFailed);
            InvokeButtonsOnProductsFetchFailed(productFetchFailed);
        }

        void OnPurchasesFetched(Orders existingOrders)
        {
            InvokeListenersOnPurchasesFetched(existingOrders);
            InvokeButtonsOnPurchasesFetched(existingOrders);
        }

        void InvokeListenersOnPurchasesFetched(Orders existingOrders)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnPurchasesFetched(existingOrders);
            }
        }

        void InvokeButtonsOnPurchasesFetched(Orders existingOrders)
        {
            foreach (var pendingOrder in existingOrders.PendingOrders)
            {
                foreach (var cartItem in pendingOrder.CartOrdered.Items())
                {
                    foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == cartItem.Product.definition.id))
                    {
                        button.OnPurchaseFetched(pendingOrder);
                    }
                }
            }

            foreach (var confirmedOrder in existingOrders.ConfirmedOrders)
            {
                foreach (var cartItem in confirmedOrder.CartOrdered.Items())
                {
                    foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == cartItem.Product.definition.id))
                    {
                        button.OnPurchaseFetched(confirmedOrder);
                    }
                }
            }
        }

        void OnPurchasesFetchFailure(PurchasesFetchFailureDescription failure)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnPurchasesFetchFailure(failure);
            }
        }

        void OnOrderPending(PendingOrder order)
        {
            InvokeOnOrderPending(order);
            ConfirmOrderIfAutomatic(order);
        }

        void InvokeOnOrderPending(PendingOrder pendingOrder)
        {
            InvokeListenersOnOrderPending(pendingOrder);
            InvokeButtonsOnOrderPending(pendingOrder);
        }

        void InvokeListenersOnOrderPending(PendingOrder pendingOrder)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnOrderPending(pendingOrder);
            }
        }

        void InvokeButtonsOnOrderPending(PendingOrder pendingOrder)
        {
            foreach (var cartItem in pendingOrder.CartOrdered.Items())
            {
                foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == cartItem.Product.definition.id))
                {
                    button.OnOrderPending(pendingOrder);
                }
            }
        }

        void ConfirmOrderIfAutomatic(PendingOrder order)
        {
            if (ShouldConfirmOrderAutomatically(order))
            {
                ConfirmOrder(order);
            }
        }

        bool ShouldConfirmOrderAutomatically(PendingOrder order)
        {
            if (m_ActiveListeners.Any(listener => listener is { automaticallyConfirmTransaction: true }))
            {
                return true;
            }

            //TODO: IAP-3100: Figure This out when we have conflicting Items
            var containsItemToNotAutoConfirm = false;
            var containsItemToAutoConfirm = false;

            foreach (var cartItem in order.CartOrdered.Items())
            {
                var matchingButton = FindMatchingButtonByProduct(cartItem.Product.definition.id);
                if (matchingButton)
                {
                    if (matchingButton is { automaticallyConfirmTransaction: true })
                    {
                        containsItemToAutoConfirm = true;
                    }
                    else
                    {
                        containsItemToNotAutoConfirm = true;
                    }
                }
            }

            if (containsItemToNotAutoConfirm && containsItemToAutoConfirm)
            {
                //TODO: IAP-3100: Figure This out when we have conflicting Items
            }

            return containsItemToAutoConfirm;
        }

        CodelessIAPButton? FindMatchingButtonByProduct(string productId)
        {
            foreach (var button in m_ActiveCodelessButtons)
            {
                if (productId.Equals(button.productId))
                {
                    return button;
                }
            }

            return null;
        }

        void ConfirmOrder(PendingOrder pendingOrder)
        {
            m_PurchasingService?.ConfirmPurchase(pendingOrder);
        }

        void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    OnOrderConfirmed(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    OnPurchaseFailed(failedOrder);
                    break;
                default:
                    Debug.unityLogger.LogIAPError($"CodelessIAPStoreListener OnPurchaseConfirmed invoked with an unexpected order type: : {order.GetType()}");
                    break;
            }
        }

        void OnOrderConfirmed(ConfirmedOrder order)
        {
            InvokeListenersOnOrderConfirmed(order);
            InvokeButtonsOnOrderConfirmed(order);
        }

        void InvokeListenersOnOrderConfirmed(ConfirmedOrder confirmedOrder)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnOrderConfirmed(confirmedOrder);
            }
        }

        void InvokeButtonsOnOrderConfirmed(ConfirmedOrder confirmedOrder)
        {
            foreach (var cartItem in confirmedOrder.CartOrdered.Items())
            {
                foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == cartItem.Product.definition.id))
                {
                    button.OnOrderConfirmed(confirmedOrder);
                }
            }
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            InvokeListenersOnPurchaseFailed(failedOrder);
            InvokeButtonsOnPurchaseFailed(failedOrder);
        }

        void InvokeListenersOnPurchaseFailed(FailedOrder failedOrder)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnPurchaseFailed(failedOrder);
            }
        }

        void InvokeButtonsOnPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder.CartOrdered != null)
            {
                foreach (var cartItem in failedOrder.CartOrdered.Items())
                {
                    foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == cartItem.Product.definition.id))
                    {
                        button.OnPurchaseFailed(failedOrder);
                    }
                }
            }
        }

        void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            InvokeListenersOnOrderDeferred(deferredOrder);
            InvokeButtonsOnOrderDeferred(deferredOrder);
        }

        void InvokeListenersOnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var listener in m_ActiveListeners)
            {
                listener.OnOrderDeferred(deferredOrder);
            }
        }

        void InvokeButtonsOnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId == product.definition.id))
                {
                    button.OnOrderDeferred(deferredOrder);
                }
            }
        }

        async Task ConnectToStore()
        {
            await m_StoreService!.Connect();
            FetchInitialProducts();
        }

        void FetchInitialProducts()
        {
            if (m_ProductService != null)
            {
                m_CatalogProvider?.FetchProducts(m_ProductService.FetchProductsWithNoRetries, DefaultStoreHelper.GetDefaultStoreName());
            }
        }

        Task AutoInitializeUnityGamingServicesIfEnabled()
        {
            return ShouldAutoInitUgs()
                ? UnityServices.InitializeAsync()
                : Task.CompletedTask;
        }

        static bool ShouldAutoInitUgs()
        {
            return Instance.m_Catalog.enableCodelessAutoInitialization &&
                   Instance.m_Catalog.enableUnityGamingServicesAutoInitialization;
        }

        /// <summary>
        /// For advanced scripted IAP actions, use this session's <typeparamref name="IStoreController"/> after
        /// initialization.
        /// </summary>
        /// <seealso cref="StoreController"/>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public IStoreController StoreController => controller;

        /// <summary>
        /// Inspect my <typeparamref name="ProductCatalog"/> for a product identifier.
        /// </summary>
        /// <param name="productID">Product identifier to look for in <see cref="m_Catalog"/>. Note this is not the
        /// store-specific identifier.</param>
        /// <returns>Whether this identifier exists in <see cref="m_Catalog"/></returns>
        /// <seealso cref="m_Catalog"/>
        public bool HasProductInCatalog(string productID)
        {
            foreach (var product in m_Catalog.allProducts)
            {
                if (product.id == productID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Access a <typeparamref name="Product"/> for this app.
        /// </summary>
        /// <param name="productID">A product identifier to find as a <typeparamref name="Product"/></param>
        /// <returns>A <typeparamref name="Product"/> corresponding to <paramref name="productID"/>, or <c>null</c> if
        /// the identifier is not available.</returns>
        public Product? GetProduct(string? productID)
        {
            var products = m_ProductService?.GetProducts();
            var product = products?.FirstOrDefault(product => product.definition.id == productID);
            if (product != null)
            {
                return product;
            }

            Debug.unityLogger.LogIAPError("CodelessIAPStoreListener attempted to get unknown product " + productID);
            return null;
        }

        /// <summary>
        /// Register an <typeparamref name="CodelessIAPButton"/> to send IAP initialization and purchasing events.
        /// Use to making IAP functionality visible to the user.
        /// </summary>
        /// <param name="button">CodelessIAPButton to register.</param>
        public void AddButton(CodelessIAPButton button)
        {
            m_ActiveCodelessButtons.Add(button);
        }

        /// <summary>
        /// Stop sending initialization and purchasing events to an <typeparamref name="CodelessIAPButton"/>. Use when disabling
        /// the button, e.g. when closing a scene containing that button and wanting to prevent the user from making any
        /// IAP events for its product.
        /// </summary>
        /// <param name="button">CodelessIAPButton to unregister.</param>
        public void RemoveButton(CodelessIAPButton button)
        {
            m_ActiveCodelessButtons.Remove(button);
        }

        /// <summary>
        /// Register an <typeparamref name="IAPListener"/> to send IAP purchasing events.
        /// </summary>
        /// <param name="listener">Listener to receive IAP purchasing events</param>
        public void AddListener(IAPListener listener)
        {
            m_ActiveListeners.Add(listener);
        }

        /// <summary>
        /// Unregister an <typeparamref name="IAPListener"/> from IAP purchasing events.
        /// </summary>
        /// <param name="listener">Listener to no longer receive IAP purchasing events</param>
        public void RemoveListener(IAPListener listener)
        {
            m_ActiveListeners.Remove(listener);
        }

        /// <summary>
        /// Purchase a product by its identifier.
        /// Sends purchase failure event with <typeparamref name="PurchaseFailureReason.PurchasingUnavailable"/>
        /// to all registered CodelessIAPButtons if not yet successfully initialized.
        /// </summary>
        /// <param name="productID">Product identifier of <typeparamref name="Product"/> to be purchased</param>
        public void InitiatePurchase(string? productID)
        {
            var product = GetProduct(productID);
            if (product != null)
            {
                try
                {
                    m_PurchasingService?.PurchaseProduct(product);
                }
                catch (Exception e)
                {
                    SendPurchaseFailedEventsToAllButtons(product);
                    Debug.unityLogger.LogIAPError(e.Message);
                }
            }
            else
            {
                SendPurchaseFailedEventsToAllButtons(product);
                Debug.unityLogger.LogIAPError("Cannot initiate purchase because product was not found for ID '" + productID + "'");
            }
        }

        void SendPurchaseFailedEventsToAllButtons(Product? product)
        {
            if (product == null)
            {
                return;
            }
            foreach (var button in m_ActiveCodelessButtons.Where(button => button.productId != null && button.productId == product?.definition.id))
            {
                var purchaseFailureDescription = new PurchaseFailureDescription(product, PurchaseFailureReason.PurchasingUnavailable, "PurchasingUnavailable");
                button.OnPurchaseFailed(purchaseFailureDescription.ConvertToFailedOrder());
            }
        }

        /// <summary>
        /// Returns the <typeparamref name="ProductCatalog"/> loaded from the project's IAP Catalog.
        /// </summary>
        /// <returns>The <typeparamref name="ProductCatalog"/>.</returns>
        public ProductCatalog GetProductCatalog()
        {
            return m_Catalog;
        }

        /// <summary>
        /// Allows outside sources to know whether the successful initialization has completed.
        /// </summary>
        /// <returns>True if the initialization has completed successfully, false otherwise.</returns>
        public bool IsInitialized()
        {
            return m_InitializationComplete;
        }
    }
}
