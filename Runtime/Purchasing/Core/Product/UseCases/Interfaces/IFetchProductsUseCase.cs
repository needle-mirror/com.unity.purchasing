#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for a class that acts out the use case of fetching products.
    /// </summary>
    interface IFetchProductsUseCase
    {
        /// <summary>
        /// Fetch a set of products based off a set of definitions.
        /// </summary>
        /// <param name="productDefinitions">The definitions of the products to be fetched.</param>
        /// <param name="fetchSuccessAction">The event called when products are successfully fetched.</param>
        /// <param name="fetchFailureAction">The event called when products could not be fetched.</param>
        /// <param name="retryPolicy">A custom retry policy that can be used for the fetch products query.</param>
        void FetchProducts(List<ProductDefinition>? productDefinitions, Action<List<Product>?> fetchSuccessAction, Action<List<ProductDefinition>?, string> fetchFailureAction, IRetryPolicy retryPolicy);
    }
}
