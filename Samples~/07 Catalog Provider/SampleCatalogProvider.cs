using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Samples.Purchasing.Core.CatalogProvider
{
    public class SampleCatalogProvider : MonoBehaviour
    {
        /// <summary>
        /// Here we create a CatalogProvider for each Catalog that we want to manage.
        /// We also declare a store, and product service needed to link our remote store with our list of products.
        /// </summary>
        UnityEngine.Purchasing.CatalogProvider m_DefaultCatalogProvider = new UnityEngine.Purchasing.CatalogProvider();
        UnityEngine.Purchasing.CatalogProvider m_SeasonalCatalogProvider = new UnityEngine.Purchasing.CatalogProvider();

        /// <summary>
        /// Here is an example of hard coded definitions of each product that will end up in the default catalog.
        /// </summary>
        const string k_Consumable50GemsSkuID = "com.example.50.gems";
        const string k_Consumable1000GemsSkuID = "com.example.1000.gems";
        const string k_NonConsumableNoAdsSkuID = "com.example.no.ads";
        const string k_SubscriptionAdventurePassSkuID = "com.example.adventure.pass";
        const string k_SubscriptionDoubleXpSkuID = "com.example.double.xp";

        public SampleCatalog catalog;


        void Awake()
        {
            UnityIAPServices.DefaultStore().OnStoreDisconnected += OnStoreDisconnected;
            InitDefaultCatalog();
            InitSeasonalCatalog();

            ConfigureProductServiceCallbacks();

            ConnectToStore();
            FetchProducts();

            FakeFetchProducts();
        }

        void OnStoreDisconnected(StoreConnectionFailureDescription storeConnectionFailureDescription)
        {
            Debug.Log($"Store disconnected. Reason: {storeConnectionFailureDescription}");
            // Optionally, update UI
        }

        /// <summary>
        /// In this method, we create a List of ProductDefiniton.
        /// Each definition contains the SkuID, and the type of product.
        /// We also declare a Dictionary, with the default skuID as key, and the store specifics IDs as values.
        /// With this, each store can have it's own definition of SkuId for the same product.
        /// In the end we add every product to the CatalogProvider created earlier.
        /// </summary>
        void InitDefaultCatalog()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new ProductDefinition(k_Consumable50GemsSkuID, ProductType.Consumable),
                new ProductDefinition(k_Consumable1000GemsSkuID, ProductType.Consumable),
                new ProductDefinition(k_NonConsumableNoAdsSkuID, ProductType.NonConsumable),
                new ProductDefinition(k_SubscriptionAdventurePassSkuID, ProductType.Subscription),
                new ProductDefinition(k_SubscriptionDoubleXpSkuID, ProductType.Subscription)
            };
            var storeSpecificIdsByProductId = new Dictionary<string, StoreSpecificIds>();
            //Here we declare each SkuID specific to each store we need
            var adventurePassIds = new StoreSpecificIds { { "com.example.google.adventure.pass", UnityEngine.Purchasing.GooglePlay.Name }, { "com.example.adventure.ios.pass", UnityEngine.Purchasing.AppleAppStore.Name }, { "com.example.adventure.macos.pass", MacAppStore.Name } };
            storeSpecificIdsByProductId.Add(k_SubscriptionAdventurePassSkuID, adventurePassIds);
            //Finally we add everything to the Catalog Provider
            m_DefaultCatalogProvider.AddProducts(initialProductsToFetch, storeSpecificIdsByProductId);

            // Build SampleCatalogProducts from initialProductsToFetch
            var sampleCatalogProducts = new List<SampleCatalogProduct>();
            foreach (var productDef in initialProductsToFetch)
            {
                StoreSpecificIds storeIds = null;
                storeSpecificIdsByProductId.TryGetValue(productDef.id, out storeIds);

                sampleCatalogProducts.Add(
                    new SampleCatalogProduct(productDef.id, productDef.type, storeIds)
                );
            }
            catalog = new SampleCatalog(sampleCatalogProducts);
        }

        /// <summary>
        /// Here is a different way to create a Catalog.
        /// Last time, we had a hard coded one, impossible to change without rebuilding the code.
        /// This time we are gonna load a json file containing the product definitions.
        /// </summary>
        void InitSeasonalCatalog()
        {
            var jsonFilePath = Application.dataPath + "/SampleCatalog.json";
            if (!File.Exists(jsonFilePath))
            {
                SampleCatalogJsonUtilities.GenerateJson(jsonFilePath);
            }

            var seasonalProducts = SampleCatalogJsonUtilities.LoadCatalogFromJson(jsonFilePath);
            foreach (var product in seasonalProducts.Products)
            {
                m_SeasonalCatalogProvider.AddProduct(product.ProductId, product.ProductType, product.StoreSpecificIds);
            }
        }

        /// <summary>
        /// Here, we connect to the store.
        /// The default store while testing in editor will be a fake store to simplify testing.
        /// It will automatically change to the right one based on the device using the built app.
        /// </summary>
        void ConnectToStore()
        {
            UnityIAPServices.DefaultStore().Connect();
        }

        /// <summary>
        /// In this method, we fetch the matching products of the catalog from the store.
        /// We need to connect to the store first using ConnectToStore to be able to get a response from it.
        /// </summary>
        void FetchProducts()
        {
            m_DefaultCatalogProvider.FetchProducts(UnityIAPServices.DefaultProduct().FetchProductsWithNoRetries);
            m_SeasonalCatalogProvider.FetchProducts(UnityIAPServices.DefaultProduct().FetchProductsWithNoRetries);
        }

        /// <summary>
        /// In this method, we would normally fetch the matching products of the catalog from the store.
        /// Here, we're passing a custom callback that simply output all the products in the console as we haven't set any store in this sample.
        /// It is a simple way to check the content of our CatalogProvider.
        /// </summary>
        void FakeFetchProducts()
        {
            m_DefaultCatalogProvider.FetchProducts(OutputRegisteredProductsInCatalog);
            m_SeasonalCatalogProvider.FetchProducts(OutputRegisteredProductsInCatalog);
        }

        /// <summary>
        /// Simple callback that outputs the content of the CatalogProvider in the Debug Console.
        /// </summary>
        /// <param name="catalog"></param>
        void OutputRegisteredProductsInCatalog(List<ProductDefinition> catalog)
        {
            foreach (var productDefinition in catalog)
            {
                if (productDefinition.storeSpecificId != null)
                {
                    Debug.Log("StoreSpecificID: " + productDefinition.storeSpecificId + "; Product Type: " + productDefinition.type);
                }
                else
                {
                    Debug.Log("Product ID: " + productDefinition.id + "; Product Type: " + productDefinition.type);
                }
            }
        }

        /// <summary>
        /// Setup the necessary callbacks for the ProductService
        /// </summary>
        void ConfigureProductServiceCallbacks()
        {
            UnityIAPServices.DefaultProduct().OnProductsFetched += OnInitialProductsFetched;
            UnityIAPServices.DefaultProduct().OnProductsFetchFailed += OnInitialProductsFetchFailed;
        }

        void OnInitialProductsFetched(List<Product> products)
        {
            // Your code here
        }

        void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            // Your code here
        }
    }
}
