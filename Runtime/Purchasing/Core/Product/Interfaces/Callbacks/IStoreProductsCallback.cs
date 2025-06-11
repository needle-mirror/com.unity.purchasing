using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// A public interface for a class that handles callbacks for retrieving products from a Store.
    /// </summary>
    public interface IStoreProductsCallback
    {
        /// <summary>
        /// Callback received when a FetchProducts call is completed successfully.
        /// </summary>
        /// <param name="products"> The list of product descriptions retrieved. </param>
        void OnProductsFetched(IReadOnlyList<ProductDescription> products);

        /// <summary>
        /// Callback received when a FetchProducts call could not be completed successfully.
        /// </summary>
        /// <param name="failureDescription"> The reason the fetch failed. </param>
        void OnProductsFetchFailed(ProductFetchFailureDescription failureDescription);
    }
}
