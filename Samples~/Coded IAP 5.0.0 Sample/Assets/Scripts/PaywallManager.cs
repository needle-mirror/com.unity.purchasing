using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace Samples.Purchasing.IAP5.Demo
{
    public class PaywallManager : MonoBehaviour
    {
        public IAPLogger m_IAPLogger;

        IStoreService m_StoreService;
        IProductService m_ProductService;
        IPurchaseService m_PurchasingService;

        ICatalogProvider m_CatalogProvider = new CatalogProvider();
        CrossPlatformValidator m_CrossPlatformValidator;

        readonly List<ProductPurchaseButtonHelper> activePurchaseButtons = new List<ProductPurchaseButtonHelper>();
        readonly IAPPaywallCallbacks m_IAPPaywallCallbacks;
        public PaywallManager()
        {
            m_IAPPaywallCallbacks = new IAPPaywallCallbacks(this);
        }

        // Here we create the services that will be used by the PaywallManager.
        protected void Awake()
        {
            CreateServices();
        }

        // Here we initialize the catalog, the IAP service, the cross platform validator and connect to the store.
        // If you want to initialize this automatically, change the function signature to "void Start()"
        public void Initialize()
        {
            InitCatalog();
            InitializeIapService();
            CreateCrossPlatformValidator();

            ConnectToStore();
        }

        void InitCatalog()
        {
            // Here are the definition of your catalog products.
            // You can modify or add products here matching the skuIds you define on your different platforms stores.
            const string k_IapConsumable30GemsSkuID = "com.unity.iap.test.30.gems";
            const string k_IapNonConsumableNoAdsSkuID = "com.unity.iap.test.no.ads";
            const string k_IapSubscriptionAdventurePassSkuID = "com.unity.iap.test.adventure.pass";

            var initialProductsToFetch = new List<ProductDefinition>
            {
                new ProductDefinition(k_IapConsumable30GemsSkuID, ProductType.Consumable),
                new ProductDefinition(k_IapNonConsumableNoAdsSkuID, ProductType.NonConsumable),
                new ProductDefinition(k_IapSubscriptionAdventurePassSkuID, ProductType.Subscription)
            };
            var storeSpecificIdsByProductId = new Dictionary<string, StoreSpecificIds>();
            var adventurePassIds = new StoreSpecificIds
            {
                {k_IapSubscriptionAdventurePassSkuID, UnityEngine.Purchasing.GooglePlay.Name},
                {k_IapSubscriptionAdventurePassSkuID, UnityEngine.Purchasing.AppleAppStore.Name}
            };
            storeSpecificIdsByProductId.Add(k_IapSubscriptionAdventurePassSkuID, adventurePassIds);

            m_CatalogProvider.AddProducts(initialProductsToFetch, storeSpecificIdsByProductId);
        }

        void CreateServices()
        {
            m_StoreService = UnityIAPServices.DefaultStore();
            m_ProductService = UnityIAPServices.DefaultProduct();
            m_PurchasingService = UnityIAPServices.DefaultPurchase();

            ConfigureServiceCallbacks();
        }

        void ConfigureServiceCallbacks()
        {
            ConfigureProductServiceCallbacks();
            ConfigurePurchasingServiceCallbacks();
        }

        void ConfigureProductServiceCallbacks()
        {
            m_ProductService.OnProductsFetched += m_IAPPaywallCallbacks.OnInitialProductsFetched;
            m_ProductService.OnProductsFetchFailed += m_IAPPaywallCallbacks.OnInitialProductsFetchFailed;
        }

        void ConfigurePurchasingServiceCallbacks()
        {
            m_PurchasingService.OnPurchasesFetched += m_IAPPaywallCallbacks.OnExistingPurchasesFetched;
            m_PurchasingService.OnPurchasesFetchFailed += m_IAPPaywallCallbacks.OnExistingPurchasesFetchFailed;
            m_PurchasingService.OnPurchasePending += m_IAPPaywallCallbacks.OnPurchasePending;
            m_PurchasingService.OnPurchaseConfirmed += m_IAPPaywallCallbacks.OnPurchaseConfirmed;
            m_PurchasingService.OnPurchaseFailed += m_IAPPaywallCallbacks.OnPurchaseFailed;
            m_PurchasingService.OnPurchaseDeferred += m_IAPPaywallCallbacks.OnOrderDeferred;
        }

        public void UpdateActivePurchaseButtons()
        {
            foreach (var button in activePurchaseButtons)
            {
                button.UpdateText();
            }
        }

        public void FetchExistingPurchases()
        {
            m_PurchasingService.FetchPurchases();
        }

        public void RestorePurchases()
        {
            m_PurchasingService.RestoreTransactions(OnTransactionsRestored);
        }

        void OnTransactionsRestored(bool success, string error)
        {
            m_IAPLogger.LogConsole("Transactions restored: " + success);
        }

        public static bool IsReceiptAvailable(Orders existingOrders)
        {
            return existingOrders != null &&
                   (existingOrders.ConfirmedOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)) ||
                    existingOrders.PendingOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)));
        }

        void InitializeIapService()
        {
            IAPService.Initialize(OnServiceInitialized, (message) =>
            {
                m_IAPLogger.LogConsole($"Initialization failed, IAP service dependency error: {message}");
            });
        }

        void CreateCrossPlatformValidator()
        {
#if !UNITY_EDITOR
            try
            {
                if (CanCrossPlatformValidate())
                {
#if !DEBUG_STOREKIT_TEST
                    m_CrossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#else
                m_CrossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#endif
                }
            }
            catch (NotImplementedException exception)
            {
                m_IAPLogger.LogConsole("===========");
                m_IAPLogger.LogConsole($"Cross Platform Validator Not Implemented: {exception}");
            }
#endif
        }

        void OnServiceInitialized()
        {
            InitializeUI();
        }

        protected void InitializeUI()
        {
            m_IAPLogger.inAppConsole.text = string.Empty;
        }

        async void ConnectToStore()
        {
            await m_StoreService.Connect();
            m_IAPLogger.LogConsole("===========");
            m_IAPLogger.LogConsole("Store Connected.");
            FetchInitialProducts();
        }

        void FetchInitialProducts()
        {
            m_CatalogProvider.FetchProducts(m_ProductService.FetchProductsWithNoRetries, DefaultStoreHelper.GetDefaultStoreName());
        }

        public void InitiatePurchase(string productId)
        {
            var product = FindProduct(productId);

            if (product != null)
            {
                m_PurchasingService?.PurchaseProduct(product);
            }
            else
            {
                m_IAPLogger.LogConsole($"The product service has no product with the ID {productId}");
            }
        }

        public Product FindProduct(string productId)
        {
            return GetFetchedProducts()?.FirstOrDefault(product => product.definition.id == productId);
        }

        public ReadOnlyObservableCollection<Product> GetFetchedProducts()
        {
            return m_ProductService?.GetProducts();
        }

        public void ConfirmOrderIfAutomatic(PendingOrder order)
        {
            if (ShouldConfirmOrderAutomatically(order))
            {
                ConfirmOrder(order);
            }
        }

        bool ShouldConfirmOrderAutomatically(PendingOrder order)
        {
            var containsItemToNotAutoConfirm = false;
            var containsItemToAutoConfirm = false;

            foreach (var cartItem in order.CartOrdered.Items())
            {
                var matchingButton = FindMatchingButtonByProduct(cartItem.Product.definition.id);

                if (matchingButton)
                {
                    if (matchingButton.consumePurchase)
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
                m_IAPLogger.LogConsole("===========");
                m_IAPLogger.LogConsole("Pending Order contains some products to not confirm. Confirming by default!");
            }

            return containsItemToAutoConfirm;
        }

        ProductPurchaseButtonHelper FindMatchingButtonByProduct(string productId)
        {
            foreach (var button in activePurchaseButtons)
            {
                if (button.productId == productId)
                {
                    return button;
                }
            }

            return null;
        }

        void ConfirmOrder(PendingOrder pendingOrder)
        {
            m_PurchasingService.ConfirmPurchase(pendingOrder);
        }

        public void RegisterButton(ProductPurchaseButtonHelper button)
        {
            activePurchaseButtons.Add(button);
        }

        public void UnregisterButton(ProductPurchaseButtonHelper button)
        {
            activePurchaseButtons.Remove(button);
        }

        public void ConfirmPendingPurchaseForId(string id)
        {
            var product = FindProduct(id);
            var order = product != null ? GetPendingOrder(product) : null;

            if (order != null)
            {
                ConfirmOrder(order);
            }
        }

        PendingOrder GetPendingOrder(Product product)
        {
            var orders = m_PurchasingService.GetPurchases();

            foreach (var order in orders)
            {
                if (order is PendingOrder pendingOrder &&
                    pendingOrder.CartOrdered.Items().First()?.Product.definition.storeSpecificId == product.definition.storeSpecificId)
                {
                    return pendingOrder;
                }
            }

            return null;
        }

        public void ValidatePurchaseIfPossible(IOrderInfo orderInfo)
        {
            if (CanCrossPlatformValidate())
            {
                ValidatePurchase(orderInfo);
            }
        }

        bool CanCrossPlatformValidate()
        {
            return IsGooglePlay() ||
                   Application.platform == RuntimePlatform.IPhonePlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.tvOS;
        }

        void ValidatePurchase(IOrderInfo orderInfo)
        {
            try
            {
                var result = m_CrossPlatformValidator.Validate(orderInfo.Receipt);

                if (IsGooglePlay())
                {
                    m_IAPLogger.LogConsole("Validated Receipt. Contents:");
                    foreach (IPurchaseReceipt productReceipt in result)
                    {
                        m_IAPLogger.LogReceiptValidation(productReceipt);
                    }
                }
                else
                {
                    m_IAPLogger.LogConsole("Validated Receipt.");
                }
            }
            catch (IAPSecurityException ex)
            {
                m_IAPLogger.LogConsole("Invalid receipt, not unlocking content. " + ex);
            }
        }

        bool IsGooglePlay()
        {
            return Application.platform == RuntimePlatform.Android && DefaultStoreHelper.GetDefaultStoreName() == UnityEngine.Purchasing.GooglePlay.Name;
        }
    }
}
