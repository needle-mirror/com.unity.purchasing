#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Catalog Provider, facilitating the retrieval of a catalog's products.
    /// </summary>
    public interface ICatalogProvider : IBaseCatalogProvider
    {
        /// <summary>
        /// The list of products in the catalog.
        /// </summary>
        /// <param name="storeName"> The name of the store to retrieve products from. If null, retrieves products without store-specific IDs.</param>
        /// <returns> A list of <see cref="ProductDefinition"/> objects representing the products in the catalog. </returns>
        public List<ProductDefinition> GetProducts(string? storeName = null);

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        public void AddProduct(string id, ProductType type);

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs);

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        /// <param name="payout"> The payout definition of the product. </param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, PayoutDefinition payout);

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        /// <param name="payouts"> The enumerator of the payout definitions of the product. </param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, IEnumerable<PayoutDefinition> payouts);

        /// <summary>
        /// Add multiple products to the configuration builder.
        /// </summary>
        /// <param name="products"> The enumerator of the product definitions to be added.
        /// The ProductDefinition.storeSpecificId will not be used since the catalog can be the same for multiple stores. Use the storeIDsByProductId instead. </param>
        /// <param name="storeIDsByProductId"> The object representing store IDs for each product id. </param>
        public void AddProducts(IEnumerable<ProductDefinition> products, Dictionary<string, StoreSpecificIds>? storeIDsByProductId = null);

        /// <summary>
        /// Fetch all the products in the catalog, asynchronously.
        /// </summary>
        /// <param name="callback"> Event containing the set of products upon completion. </param>
        /// <param name="storeName"> The name of the store the products are from. </param>
        void FetchProducts(Action<List<ProductDefinition>> callback, string storeName);
    }
}
