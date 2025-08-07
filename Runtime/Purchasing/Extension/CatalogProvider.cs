#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    using storeSpecificIDsByProductID = Dictionary<string, string>;

    /// <summary>
    /// Represents a catalog provider that manages product definitions and their store-specific IDs.
    /// </summary>
    public class CatalogProvider : ICatalogProvider
    {
        Dictionary<string?, storeSpecificIDsByProductID> m_StoreSpecificIds = new();
        List<ProductDefinition> m_Products = new();

        /// <summary>
        /// Gets the product definitions from the catalog provider.
        /// </summary>
        /// <param name="storeName">The name of the store from which to get the products.</param>
        /// <returns>A list of product definitions.</returns>
        public List<ProductDefinition> GetProducts(string? storeName = null)
        {
            if (storeName != null)
            {
                UpdateStoreSpecificIDs(storeName);
            }

            return m_Products;
        }

        /// <summary>
        /// Adds a product to the catalog provider with the specified ID and type.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <param name="type">The type of the product.</param>
        public void AddProduct(string id, ProductType type)
        {
            AddProduct(id, type, null);
        }

        /// <summary>
        /// Adds a product to the catalog provider with the specified ID, type, and store-specific IDs.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <param name="type">The type of the product.</param>
        /// <param name="storeIDs">The store-specific IDs for the product.</param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs)
        {
            AddProduct(id, type, storeIDs, (IEnumerable<PayoutDefinition>)null!);
        }

        /// <summary>
        /// Adds a product to the catalog provider with the specified ID, type, store-specific IDs, and payout definition.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <param name="type">The type of the product.</param>
        /// <param name="storeIDs">The store-specific IDs for the product.</param>
        /// <param name="payout">The payout definition for the product.</param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, PayoutDefinition payout)
        {
            AddProduct(id, type, storeIDs, new List<PayoutDefinition> { payout });
        }

        /// <summary>
        /// Adds a product to the catalog provider with the specified ID, type, store-specific IDs, and a collection of payout definitions.
        /// </summary>
        /// <param name="id">The ID of the product.</param>
        /// <param name="type">The type of the product.</param>
        /// <param name="storeIDs">The store-specific IDs for the product.</param>
        /// <param name="payouts">The collection of payout definitions for the product.</param>
        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, IEnumerable<PayoutDefinition>? payouts)
        {
            // Extract our store specific ID if present, according to the current store.
            var specificId = AddStoreSpecificIds(id, storeIDs);

            var product = new ProductDefinition(id, specificId, type);
            if (payouts != null)
            {
                product.SetPayouts(payouts);
            }

            m_Products.Add(product);
        }

        /// <summary>
        /// Adds store-specific IDs to the product ID and updates the internal store-specific IDs dictionary.
        /// </summary>
        /// <param name="id">The product ID to which store-specific IDs will be added.</param>
        /// <param name="storeIDs">The store-specific IDs to be added for the product.</param>
        /// <returns>Returns the last store-specific ID added for the product.</returns>
        string AddStoreSpecificIds(string id, StoreSpecificIds? storeIDs)
        {
            if (storeIDs == null)
            {
                return id;
            }

            var lastSpecificId = id;
            using var storeSpecificIDsByStore = storeIDs.GetEnumerator();
            while (storeSpecificIDsByStore.MoveNext())
            {
                var cur = storeSpecificIDsByStore.Current;

                if (!m_StoreSpecificIds.ContainsKey(cur.Key))
                {
                    m_StoreSpecificIds.Add(cur.Key, new storeSpecificIDsByProductID());
                }

                m_StoreSpecificIds[cur.Key][id] = cur.Value;
                lastSpecificId = cur.Value;
            }

            return lastSpecificId;
        }

        /// <summary>
        /// Adds multiple products to the catalog provider with their definitions and optional store-specific IDs.
        /// </summary>
        /// <param name="productsDefinitions">Product definitions to be added to the catalog.</param>
        /// <param name="storeIDsByProductId">Store-specific IDs mapped by product ID, or null if not applicable.</param>
        public void AddProducts(IEnumerable<ProductDefinition> productsDefinitions, Dictionary<string, StoreSpecificIds>? storeIDsByProductId)
        {
            foreach (var product in productsDefinitions)
            {
                m_Products.Add(product);
                if (storeIDsByProductId != null && storeIDsByProductId.TryGetValue(product.id, out var storeSpecificIds))
                {
                    AddStoreSpecificIds(product.id, storeSpecificIds);
                }
            }
        }

        /// <summary>
        /// Fetches the product definitions from the catalog provider and invokes the callback with the list of products.
        /// </summary>
        /// <param name="callback">The FetchProduct callback to invoke with the list of products.</param>
        public void FetchProducts(Action<List<ProductDefinition>> callback)
        {
            FetchProducts(callback, DefaultStoreHelper.GetDefaultStoreName());
        }

        /// <summary>
        /// Fetches the product definitions from the catalog provider for a specific store and invokes the FetchProducts
        /// callback with the list of products.
        /// </summary>
        /// <param name="callback">The FetchProduct callback to invoke with the list of products.</param>
        /// <param name="storeName">The name of the store from which to fetch the products.</param>
        public void FetchProducts(Action<List<ProductDefinition>> callback, string storeName)
        {
            var productDefinitions = GetProducts(storeName);
            callback(productDefinitions);
        }

        void UpdateStoreSpecificIDs(string storeName)
        {
            foreach (var product in m_Products)
            {
                if (m_StoreSpecificIds.TryGetValue(storeName, out var storeIDs) &&
                    storeIDs.TryGetValue(product.id, out var storeSpecificId))
                {
                    product.storeSpecificId = storeSpecificId;
                }
            }
        }
    }
}
