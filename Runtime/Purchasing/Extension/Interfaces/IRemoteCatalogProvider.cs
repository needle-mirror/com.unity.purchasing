#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Remote Catalog Provider, facilitating the retrieval of the remote catalog's products.
    /// </summary>
    public interface IRemoteCatalogProvider : IBaseCatalogProvider
    {
        /// <summary>
        /// Fetch the remote catalog and returns the list of products in the catalog.
        /// </summary>
        /// <returns>Returns <see cref="FetchRemoteCatalogResult"/>.</returns>
        public Task<FetchRemoteCatalogResult> FetchRemoteCatalog();

        /// <summary>
        /// The list of products in the catalog.
        /// </summary>
        /// <param name="storeName"> The name of the store to retrieve products from. If null, retrieves products without store-specific IDs.</param>
        /// <returns> A list of <see cref="ProductDefinition"/> objects representing the products in the catalog. </returns>
        public List<ProductDefinition> GetProducts(string? storeName = null);

        /// <summary>
        /// Fetch all the products in the catalog, asynchronously.
        /// </summary>
        /// <param name="callback"> Event containing the set of products upon completion. </param>
        /// <param name="storeName"> The name of the store the products are from. </param>
        void FetchProducts(Action<List<ProductDefinition>> callback, string storeName);
    }
}
