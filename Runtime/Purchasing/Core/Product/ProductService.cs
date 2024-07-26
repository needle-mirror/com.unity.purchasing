#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The core API implementation of the Product Service, the main class used to fetch and store products from the Store Service.
    /// </summary>
    public class ProductService : IProductService
    {
        private static readonly IRetryPolicy DefaultRetryPolicy = new ExponentialBackOffRetryPolicy();
        readonly IFetchProductsUseCase m_FetchProductsUseCase;
        readonly ObservableCollection<Product> m_Products = new ObservableCollection<Product>();
        readonly ReadOnlyObservableCollection<Product> m_ProductsReadOnly;
        readonly IStoreWrapper m_StoreWrapper;

        event Action<List<Product>>? OnProductsUpdated;
        event Action<ProductFetchFailed>? OnProductsFetchFailed;

        internal ProductService(IFetchProductsUseCase fetchProductsUseCase, IStoreWrapper storeWrapper)
        {
            m_FetchProductsUseCase = fetchProductsUseCase;
            m_ProductsReadOnly = new ReadOnlyObservableCollection<Product>(m_Products);
            m_StoreWrapper = storeWrapper;
        }

        public IAppleStoreExtendedProductService? Apple => this as IAppleStoreExtendedProductService;
        public IAmazonAppsExtendedProductService? Amazon => this as IAmazonAppsExtendedProductService;

        public void FetchProductsWithNoRetries(List<ProductDefinition>? productDefinitions)
        {
            CheckStoreConnectionState();
            FetchProducts(productDefinitions, null);
        }

        public void FetchProducts(List<ProductDefinition>? productDefinitions, IRetryPolicy? retryPolicy)
        {
            CheckStoreConnectionState();
            if (OnProductsUpdated == null)
            {
                throw new ProductFetchException("No Products Updated actions set. No success callbacks will be sent for this Fetch. Please set an action via `AddProductsUpdatedAction()`");
            }

            if (OnProductsFetchFailed == null)
            {
                throw new ProductFetchException("No Products Fetch Failed actions set. No error callbacks will be sent for this Fetch. Please set an action via `AddProductsFetchFailedAction()`");
            }

            m_FetchProductsUseCase.FetchProducts(productDefinitions, HandleProductsFetched, HandleProductsFetchFailed, retryPolicy ?? DefaultRetryPolicy);
        }

        /// <summary>
        /// Gets the collection of all products that have been fetched successfully.
        /// </summary>
        /// <returns>An observable collection of the products already fetched.</returns>
        public ReadOnlyObservableCollection<Product> GetProducts()
        {
            CheckStoreConnectionState();
            return m_ProductsReadOnly;
        }

        void HandleProductsFetched(List<Product>? fetchedProducts)
        {
            CheckStoreConnectionState();
            fetchedProducts ??= new List<Product>();

            foreach (var fetchedProduct in fetchedProducts)
            {
                m_Products.Add(fetchedProduct);
            }

            OnProductsUpdated?.Invoke(fetchedProducts);
        }

        void HandleProductsFetchFailed(List<ProductDefinition>? fetchedProducts, string reason)
        {
            CheckStoreConnectionState();
            var failure = new ProductFetchFailed(fetchedProducts ?? new List<ProductDefinition>(), reason);

            OnProductsFetchFailed?.Invoke(failure);
        }

        /// <summary>
        /// Add an action to be called when products are successfully fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        public void AddProductsUpdatedAction(Action<List<Product>> updatedAction)
        {
            OnProductsUpdated -= updatedAction;
            OnProductsUpdated += updatedAction;
        }

        /// <summary>
        /// Add an action to be called when an attempt to fetch products has failed.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        public void AddProductsFetchFailedAction(Action<ProductFetchFailed> failedAction)
        {
            OnProductsFetchFailed -= failedAction;
            OnProductsFetchFailed += failedAction;
        }

        /// <summary>
        /// Remove an action to be called when products are successfully fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        public void RemoveProductsUpdatedAction(Action<List<Product>> updatedAction)
        {
            OnProductsUpdated -= updatedAction;
        }

        /// <summary>
        /// Remove an action to be called when an attempt to fetch products has failed.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        public void RemoveProductsFetchFailedAction(Action<ProductFetchFailed> failedAction)
        {
            OnProductsFetchFailed -= failedAction;
        }

        void CheckStoreConnectionState()
        {
            if (m_StoreWrapper.GetStoreConnectionState() != ConnectionState.Connected)
            {
                throw new StoreConnectionException(
                    "Product Service couldn't execute its task, store connection state: " + m_StoreWrapper.GetStoreConnectionState()
                );
            }
        }
    }
}
