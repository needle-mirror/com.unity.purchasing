using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides helper methods to retrieve products by
    /// store independent/store specific id.
    /// </summary>
    public class ProductCollection
    {
        private Dictionary<string, Product> m_IdToProduct;
        private Dictionary<string, Product> m_StoreSpecificIdToProduct;

        internal ProductCollection(Product[] products)
        {
            AddProducts(products);
        }

        internal void AddProducts(IEnumerable<Product> products)
        {
            set.UnionWith(products);
            all = set.ToArray();
            m_IdToProduct = all.ToDictionary(x => x.definition.id);
            m_StoreSpecificIdToProduct = all.ToDictionary(x => x.definition.storeSpecificId);
        }

        /// <summary>
        /// The hash set of all products
        /// </summary>
        public HashSet<Product> set { get; } = new HashSet<Product>();

        /// <summary>
        /// The array of all products
        /// </summary>
        public Product[] all { get; private set; }

        /// <summary>
        /// Gets a product matching an id
        /// </summary>
        /// <param name="id"> The id of the desired product </param>
        /// <returns> The product matching the id, or null if not found </returns>
        public Product WithID(string id)
        {
            m_IdToProduct.TryGetValue(id, out var result);
            return result;
        }

        /// <summary>
        /// Gets a product matching a store-specific id
        /// </summary>
        /// <param name="id"> The store-specific id of the desired product </param>
        /// <returns> The product matching the id, or null if not found </returns>
        public Product WithStoreSpecificID(string id)
        {
            Product result = null;
            if (id != null)
            {
                m_StoreSpecificIdToProduct.TryGetValue(id, out result);
            }
            return result;
        }
    }
}
