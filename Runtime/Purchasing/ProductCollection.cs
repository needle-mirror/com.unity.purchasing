using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides helper methods to retrieve products by
    /// store independent/store specific id.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class ProductCollection
    {
        private Dictionary<string, Product> m_IdToProduct;
        private Dictionary<string, Product> m_StoreSpecificIdToProduct;

        /// <summary>
        /// The hash set of all products
        /// </summary>
        public HashSet<Product> set => new(all);

        /// <summary>
        /// The array of all products
        /// </summary>
        public Product[] all => ProductServiceProvider.GetDefaultProductService().GetProducts().ToArray();

        /// <summary>
        /// Gets a product matching an id
        /// </summary>
        /// <param name="id"> The id of the desired product </param>
        /// <returns> The product matching the id, or null if not found </returns>
        public Product WithID(string id)
        {
            var idToProduct = all.ToDictionary(x => x.definition.id);
            idToProduct.TryGetValue(id, out var result);
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
                var storeSpecificIdToProduct = all.ToDictionary(x => x.definition.storeSpecificId);
                storeSpecificIdToProduct.TryGetValue(id, out result);
            }
            return result;
        }
    }
}
