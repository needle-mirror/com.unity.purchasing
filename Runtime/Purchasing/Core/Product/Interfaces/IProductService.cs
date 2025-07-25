#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A service responsible for fetching and storing products available for purchase.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Apple Specific Product Extensions
        /// </summary>
        IAppleStoreExtendedProductService? Apple { get; }

        /// <summary>
        /// Fetches products matching the set of definitions passed as input.
        /// Be sure to first register callbacks via `AddProductsUpdatedAction` and `AddProductsFetchFailedAction`.
        /// </summary>
        /// <param name="productDefinitions">The definitions of the products to be fetched.</param>
        void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions);

        /// <summary>
        /// Fetches products matching the set of definitions passed as input.
        /// Be sure to first register your callbacks via `OnProductsFetched` and `OnProductsFetchFailed`.
        /// </summary>
        /// <param name="productDefinitions">The definitions of the products to be fetched.</param>
        /// <param name="retryPolicy">A custom retry policy that can be used for the fetch products query.
        /// If unspecified, an exponential backoff retry policy will be used.</param>
        void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy? retryPolicy = null);

        /// <summary>
        /// Gets the collection of all products that have been fetched successfully.
        /// </summary>
        /// <returns>An observable collection of the products already fetched.</returns>
        ReadOnlyObservableCollection<Product> GetProducts();

        /// <summary>
        /// Gets a product by its product ID. If no product is found with the specified product ID,
        /// attempts to locate a product with a matching store-specific product ID. Returns null if no matching product is found.
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <returns>The matching product if found, otherwise returns null.</returns>
        Product? GetProductById(string productId);

        /// <summary>
        /// Callback invoked with products that are successfully fetched.
        /// </summary>
        event Action<List<Product>>? OnProductsFetched;

        /// <summary>
        /// Callback invoked when an attempt to fetch products has failed or when a subset of products failed to be fetched.
        /// </summary>
        event Action<ProductFetchFailed>? OnProductsFetchFailed;
    }
}
