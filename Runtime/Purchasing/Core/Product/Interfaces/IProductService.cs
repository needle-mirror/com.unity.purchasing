#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A service responsible for fetching and storing products available for purchase.
    /// Tasks requiring a connection to Stores (Apple App Store or Google Play Store for example)
    /// executed without ConnectionState.Connected will throw a StoreConnectionException.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Apple Specific Product Extensions
        /// </summary>
        public IAppleStoreExtendedProductService? Apple { get; }

        /// <summary>
        /// Amazon Specific Product Extensions
        /// </summary>
        public IAmazonAppsExtendedProductService? Amazon { get; }

        /// <summary>
        /// Fetches products matching the set of definitions passed as input.
        /// </summary>
        /// <param name="productDefinitions">The definitions of the products to be fetched.</param>
        void FetchProductsWithNoRetries(List<ProductDefinition>? productDefinitions);

        /// <summary>
        /// Fetches products matching the set of definitions passed as input.
        /// </summary>
        /// <param name="productDefinitions">The definitions of the products to be fetched.</param>
        /// <param name="retryPolicy">A custom retry policy that can be used for the fetch products query.
        /// If unspecified, an exponential backoff retry policy will be used.</param>
        void FetchProducts(List<ProductDefinition>? productDefinitions, IRetryPolicy? retryPolicy);

        /// <summary>
        /// Gets the collection of all products that have been fetched successfully.
        /// </summary>
        /// <returns>An observable collection of the products already fetched.</returns>
        ReadOnlyObservableCollection<Product> GetProducts();

        /// <summary>
        /// Add an action to be called when products are successfully fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be added to the list of callbacks.</param>
        void AddProductsUpdatedAction(Action<List<Product>> updatedAction);

        /// <summary>
        /// Add an action to be called when an attempt to fetch products has failed.
        /// </summary>
        /// <param name="failedAction">The action to be added to the list of callbacks.</param>
        void AddProductsFetchFailedAction(Action<ProductFetchFailed> failedAction);

        /// <summary>
        /// Remove an action to be called when products are successfully fetched.
        /// </summary>
        /// <param name="updatedAction">The action to be removed from the list of callbacks.</param>
        void RemoveProductsUpdatedAction(Action<List<Product>> updatedAction);

        /// <summary>
        /// Remove an action to be called when an attempt to fetch products has failed.
        /// </summary>
        /// <param name="failedAction">The action to be removed from the list of callbacks.</param>
        void RemoveProductsFetchFailedAction(Action<ProductFetchFailed> failedAction);
    }
}
