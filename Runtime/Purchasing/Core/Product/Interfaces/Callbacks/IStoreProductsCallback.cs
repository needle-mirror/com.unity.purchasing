using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// A public interface for a class that handles callbacks for retrieving products from a Store.
    /// </summary>
    public interface IStoreProductsCallback
    {
        /// <summary>
        /// Callback received when a RetrieveProducts call is completed successfully.
        /// </summary>
        /// <param name="products"> The list of product descriptions retrieved. </param>
        void OnProductsRetrieved(IReadOnlyList<ProductDescription> products);

        /// <summary>
        /// Callback received when a RetrieveProducts call could not be completed successfully.
        /// </summary>
        /// <param name="failureDescription"> The reason the fetch failed. </param>
        void OnProductsRetrieveFailed(ProductFetchFailureDescription failureDescription);
    }
}
