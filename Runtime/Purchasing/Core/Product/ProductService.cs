#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The core API implementation of the Product Service, the main class used to fetch and store products from the Store Service.
    /// </summary>
    class ProductService : IProductService
    {
        static readonly IRetryPolicy k_DefaultRetryPolicy = new ExponentialBackOffRetryPolicy();
        readonly IFetchProductsUseCase m_FetchProductsUseCase;
        readonly IStoreWrapper m_StoreWrapper;
        readonly IProductCache m_ProductCache;

        public event Action<List<Product>>? OnProductsFetched;
        public event Action<ProductFetchFailed>? OnProductsFetchFailed;

        internal ProductService(IFetchProductsUseCase fetchProductsUseCase, IStoreWrapper storeWrapper)
        {
            m_FetchProductsUseCase = fetchProductsUseCase;
            m_StoreWrapper = storeWrapper;
            m_ProductCache = storeWrapper.instance.ProductCache;
        }

        public IAppleStoreExtendedProductService? Apple => this as IAppleStoreExtendedProductService;

        public void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions)
        {
            FetchProducts(productDefinitions, null);
        }

        public void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy? retryPolicy)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (OnProductsFetched == null)
            {
                Debug.unityLogger.LogIAPWarning("IProductService.FetchProducts called without a callback defined for IProductService.OnProductsFetched.");
            }

            if (OnProductsFetchFailed == null)
            {
                Debug.unityLogger.LogIAPWarning("IProductService.FetchProducts called without a callback defined for IProductService.OnProductsFetchFailed.");
            }
#endif

            try
            {
                CheckStoreConnectionState();
                m_FetchProductsUseCase.FetchProducts(productDefinitions, HandleProductsFetched, HandleProductsFetchFailed, retryPolicy ?? k_DefaultRetryPolicy);
            }
            catch (Exception e)
            {
                HandleProductsFetchFailed(productDefinitions, e.Message);
            }
        }

        /// <summary>
        /// Gets the collection of all products that have been fetched successfully.
        /// </summary>
        /// <returns>An observable collection of the products already fetched.</returns>
        public ReadOnlyObservableCollection<Product> GetProducts()
        {
            return m_ProductCache.GetProducts();
        }

        /// <summary>
        /// Gets a product by its product ID. If no product is found with the specified product ID,
        /// attempts to locate a product with a matching store-specific product ID. Returns null if no matching product is found.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <returns>The matching product if found, otherwise returns null.</returns>
        public Product? GetProductById(string productId)
        {
            return m_ProductCache.Find(productId);
        }

        void HandleProductsFetched(List<Product>? fetchedProducts)
        {
            fetchedProducts ??= new List<Product>();

            m_ProductCache.Add(fetchedProducts);

            OnProductsFetched?.Invoke(fetchedProducts);
        }

        void HandleProductsFetchFailed(List<ProductDefinition>? fetchedProducts, string reason)
        {
            var failure = new ProductFetchFailed(fetchedProducts ?? new List<ProductDefinition>(), reason);

            OnProductsFetchFailed?.Invoke(failure);
        }

        void CheckStoreConnectionState()
        {
            if (m_StoreWrapper.GetStoreConnectionState() != ConnectionState.Connected)
            {
                throw new Exception(
                    "Product Service couldn't execute its task, store connection state: " + m_StoreWrapper.GetStoreConnectionState()
                );
            }
        }
    }
}
