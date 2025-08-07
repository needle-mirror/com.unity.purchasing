#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An abstract store service to extend an existing Product Service which will handle all of the basic IProductService implementations
    /// The main purpose of this is to allow a custom store to add implementations of extended features to this service.
    /// The calls to IProductService are kept virtual so that the derivations of the base store implementing them can be added to or overridden.
    /// </summary>
    public abstract class ExtensibleProductService : IProductService
    {
        IProductService m_BaseInternalProductService;

        /// <summary>
        /// Constructor to be used by derived classes
        /// </summary>
        /// <param name="baseProductService"> The base service implementation which implements IProductService </param>
        protected ExtensibleProductService(IProductService baseProductService)
        {
            m_BaseInternalProductService = baseProductService;
        }

        /// <summary>
        /// Apple Specific Product Extensions
        /// </summary>
        public virtual IAppleStoreExtendedProductService? Apple => m_BaseInternalProductService.Apple;

        /// <summary>
        /// Fetches products without any retry policy.
        /// </summary>
        /// <param name="productDefinitions">The list of product definitions to fetch.</param>
        public virtual void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions)
        {
            m_BaseInternalProductService.FetchProductsWithNoRetries(productDefinitions);
        }

        /// <summary>
        /// Fetches products with an optional retry policy.
        /// </summary>
        /// <param name="productDefinitions">The list of product definitions to fetch.</param>
        /// <param name="retryPolicy">The retry policy to use for fetching products, if any.</param>
        public virtual void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy? retryPolicy = null)
        {
            m_BaseInternalProductService.FetchProducts(productDefinitions, retryPolicy);
        }

        /// <summary>
        /// Gets the products from the internal product service.
        /// </summary>
        /// <returns>A read-only collection of products.</returns>
        public virtual ReadOnlyObservableCollection<Product> GetProducts()
        {
            return m_BaseInternalProductService.GetProducts();
        }

        /// <summary>
        /// Gets a product by its identifier.
        /// </summary>
        /// <param name="productId">The identifier of the product to retrieve.</param>
        /// <returns>The product with the specified identifier, or null if not found.</returns>
        public virtual Product? GetProductById(string productId)
        {
            return m_BaseInternalProductService.GetProductById(productId);
        }

        /// <summary>
        /// Event that is triggered when products are fetched successfully.
        /// </summary>
        public event Action<List<Product>>? OnProductsFetched
        {
            add => m_BaseInternalProductService.OnProductsFetched += value;
            remove => m_BaseInternalProductService.OnProductsFetched -= value;
        }

        /// <summary>
        /// Event that is triggered when product fetching fails.
        /// </summary>
        public event Action<ProductFetchFailed>? OnProductsFetchFailed
        {
            add => m_BaseInternalProductService.OnProductsFetchFailed += value;
            remove => m_BaseInternalProductService.OnProductsFetchFailed -= value;
        }
    }
}
