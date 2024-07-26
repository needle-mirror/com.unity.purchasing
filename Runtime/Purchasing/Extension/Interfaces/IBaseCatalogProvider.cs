using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Abstract Catalog Provider, facilitating the retrieval of a catalog's products.
    /// </summary>
    public interface IBaseCatalogProvider
    {
        /// <summary>
        /// Fetch all the products in the catalog, asynchronously.
        /// </summary>
        /// <param name="callback"> Event containing the set of products upon completion. </param>
        void FetchProducts(Action<List<ProductDefinition>> callback);
    }
}
