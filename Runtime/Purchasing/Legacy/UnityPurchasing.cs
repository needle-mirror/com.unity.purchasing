using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The core abstract implementation for Unity Purchasing.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public class UnityPurchasing
    {
        internal static bool shouldFetchProductsAtInit = true;
        internal static IStoreListener m_StoreListener;
        internal static ConfigurationBuilder m_ConfigurationBuilder;

        internal static PurchasingManager m_PurchasingManager = new PurchasingManager();
        /// <summary>
        /// The main initialization call for Unity Purchasing.
        /// </summary>
        /// <param name="listener"> The <c>IDetailedStoreListener</c> to receive callbacks for future transactions </param>
        /// <param name="builder"> The <c>ConfigurationBuilder</c> containing the product definitions mapped to stores </param>
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
            productService.AddProductsUpdatedAction(list =>
            {
                storeListener.OnInitialized(m_PurchasingManager, new ExtensionProvider());
            });

            productService.AddProductsFetchFailedAction(failed =>
            {
                storeListener.OnInitializeFailed(InitializationFailureReason.PurchasingUnavailable, failed.FailureReason);
            });
        }

        static async void ConnectToStoreAndFetchProducts()
        {
            try
            {
                var storeService = StoreServiceProvider.GetDefaultStoreService();
                await storeService.ConnectAsync();
                if (shouldFetchProductsAtInit)
                {
                    FetchProducts(m_StoreListener, m_ConfigurationBuilder);
                }
            }
            catch (StoreConnectionException exception)
            {
                Debug.Log(exception);
            }
        }
    }
}
