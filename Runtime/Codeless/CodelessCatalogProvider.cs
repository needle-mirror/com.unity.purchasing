using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Helps to set up CatalogProvider for Codeless IAP.
    /// </summary>
    public static class CodelessCatalogProvider
    {
        /// <summary>
        /// Populate a CatalogProvider with products from a ProductCatalog
        /// </summary>
        /// <param name="catalog">Source of product identifiers and payouts</param>
        /// <returns>A CatalogProvider populated with the products from the ProductCatalog</returns>
        public static CatalogProvider PopulateCatalogProvider(ProductCatalog catalog)
        {
            var catalogProvider = new CatalogProvider();
            foreach (var product in catalog.allProducts)
            {
                if (product == null)
                {
                    continue;
                }

                var storeSpecificIds = new StoreSpecificIds();
                foreach (var storeSpecificId in product?.allStoreIDs)
                {
                    storeSpecificIds.Add(storeSpecificId.id, storeSpecificId.store);
                }

                var payoutDefinitions = new List<PayoutDefinition>();
                foreach (var payout in product.Payouts)
                {
                    payoutDefinitions.Add(new PayoutDefinition(payout.typeString, payout.subtype, payout.quantity, payout.data));
                }

                catalogProvider.AddProduct(product.id, product.type, storeSpecificIds, payoutDefinitions);
            }

            return catalogProvider;
        }
    }
}
