using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The core abstract implementation for Unity Purchasing.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class UnityPurchasing
    {
        internal static ConfigurationBuilder m_ConfigurationBuilder;
        internal static IStoreListener m_StoreListener;
        internal static bool shouldFetchProductsAtInit = true;
        static bool isInitialized = false;

        internal static PurchasingManager m_PurchasingManager = new PurchasingManager();
        /// <summary>
        /// The main initialization call for Unity Purchasing.
        /// </summary>
        /// <param name="storeListener"> The <c>IDetailedStoreListener</c> to receive callbacks for future transactions </param>
        /// <param name="configurationBuilder"> The <c>ConfigurationBuilder</c> containing the product definitions mapped to stores </param>
        public static void Initialize(IStoreListener storeListener, ConfigurationBuilder configurationBuilder)
        {
            m_StoreListener = storeListener;
            m_ConfigurationBuilder = configurationBuilder;
            ConnectToStoreAndFetchProducts();
        }

        static void FetchProducts(IStoreListener storeListener, ConfigurationBuilder configurationBuilder)
        {
            var productService = ProductServiceProvider.GetDefaultProductService();
            AddProductServiceListeners(storeListener, productService);
            productService.FetchProducts(configurationBuilder.m_CatalogProvider.GetProducts(), new MaximumNumberOfAttemptsRetryPolicy(5));
        }


        static void AddProductServiceListeners(IStoreListener storeListener, IProductService productService)
        {
            productService.OnProductsFetched += _ =>
            {
                if (!isInitialized)
                {
                    PurchaseServiceProvider.GetDefaultPurchaseService().FetchPurchases();
                    storeListener.OnInitialized(m_PurchasingManager, new ExtensionProvider());
                    isInitialized = true;
                }
            };

            productService.OnProductsFetchFailed += failed =>
            {
                if (!isInitialized)
                {
                    if (failed.FailureReason == ErrorMessages.FetchProductsRetrieveProductsFailed)
                    {
                        storeListener.OnInitializeFailed(InitializationFailureReason.PurchasingUnavailable, failed.FailureReason);
                    }
                }
            };
        }

        static async void ConnectToStoreAndFetchProducts()
        {
            var storeService = StoreServiceProvider.GetDefaultStoreService();
            await storeService.Connect();
            if (shouldFetchProductsAtInit)
            {
                FetchProducts(m_StoreListener, m_ConfigurationBuilder);
            }
        }
    }
}
