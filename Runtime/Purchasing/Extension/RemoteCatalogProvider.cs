#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine.Purchasing.CatalogListings;
using UnityEngine.Purchasing.Extension;
using CatalogProductType = UnityEngine.Purchasing.CatalogListings.CatalogProductType;

namespace UnityEngine.Purchasing
{
    using storeSpecificIDsByCatalogListingId = Dictionary<string, string>;

    /// <summary>
    /// Represents the result of fetching the remote catalog.
    /// </summary>
    public sealed class FetchRemoteCatalogResult
    {
        public bool Success;
        public Exception? Exception;
    }

    /// <summary>
    /// Represents a catalog provider that obtains the remote catalog and manages product definitions with their store-specific IDs.
    /// </summary>
    public class RemoteCatalogProvider : IRemoteCatalogProvider
    {
        Dictionary<string?, storeSpecificIDsByCatalogListingId> m_StoreSpecificIds = new();
        List<ProductDefinition> m_Products = new();

        public async Task<FetchRemoteCatalogResult> FetchRemoteCatalog()
        {
            try
            {
                var catalogListingClient = CatalogListingClientProvider.Instance();
                if (!catalogListingClient.IsAvailable)
                {
                    return new FetchRemoteCatalogResult {Success = false, Exception = new ServicesInitializationException("Unity Services uninitialized. Please call UnityServices.InitializeAsync() to initialize.")};
                }

                var catalogListingResult = await catalogListingClient.GetCatalogListings();
                if (!catalogListingResult.CompletedSuccessfully)
                {
                    var failedAt = catalogListingResult.LastFailedAfter ?? "<first page>";
                    return new FetchRemoteCatalogResult
                    {
                        Success = false,
                        Exception = new Exception($"Failed to fetch full remote catalog; paging stopped after cursor {failedAt}.")
                    };
                }

                foreach (var catalogListingDto in catalogListingResult.Results)
                {
                    var storeIds = new StoreSpecificIds();
                    foreach (var storeIdOverride in catalogListingDto.StoreIdOverrides)
                    {
                        var storeName = StoreNameFromCatalogStore(storeIdOverride.Store);
                        if (storeName == null)
                        {
                            continue;
                        }
                        storeIds.Add(storeIdOverride.Value, storeName);
                    }
                    AddProduct(
                        catalogListingDto.USku,
                        ProductTypeFromCatalogType(catalogListingDto.Type),
                        catalogListingDto.CatalogListingId,
                        storeIds
                    );
                }
            }
            catch (Exception e)
            {
                return new FetchRemoteCatalogResult {Success = false, Exception = e};
            }

            return new FetchRemoteCatalogResult {Success = true};
        }

        static string? StoreNameFromCatalogStore(CatalogStore? store)
        {
            return store switch
            {
                CatalogStore.Apple => AppleAppStore.Name,
                CatalogStore.Google => GooglePlay.Name,
                CatalogStore.AppleMacos => MacAppStore.Name,
                CatalogStore.Xbox => XboxStore.Name,
                _ => null
            };
        }

        ProductType ProductTypeFromCatalogType(CatalogProductType catalogType)
        {
            return catalogType switch
            {
                CatalogProductType.Consumable => ProductType.Consumable,
                CatalogProductType.NonConsumable => ProductType.NonConsumable,
                CatalogProductType.Subscription => ProductType.Subscription,
                _ => ProductType.Unknown
            };
        }

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

        internal void AddProduct(string id, ProductType type, string? catalogListingId, StoreSpecificIds? storeIDs)
        {
            var product = catalogListingId == null
                ? new ProductDefinition(id, id, type)
                : new ProductDefinition(id, id, type, catalogListingId);
            // Key the per-store override map by the definition's catalogListingId — not by uSku —
            // so multi-listing products (same uSku, distinct catalogListingId, distinct storeSpecificId)
            // don't clobber each other's overrides.
            product.storeSpecificId = AddStoreSpecificIds(product.catalogListingId, id, storeIDs);
            m_Products.Add(product);
        }

        string AddStoreSpecificIds(string catalogListingId, string fallback, StoreSpecificIds? storeIDs)
        {
            if (storeIDs == null)
            {
                return fallback;
            }

            var lastSpecificId = fallback;
            using var storeSpecificIDsByStore = storeIDs.GetEnumerator();
            while (storeSpecificIDsByStore.MoveNext())
            {
                var cur = storeSpecificIDsByStore.Current;

                if (!m_StoreSpecificIds.ContainsKey(cur.Key))
                {
                    m_StoreSpecificIds.Add(cur.Key, new storeSpecificIDsByCatalogListingId());
                }

                m_StoreSpecificIds[cur.Key][catalogListingId] = cur.Value;
                lastSpecificId = cur.Value;
            }

            return lastSpecificId;
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
                    storeIDs.TryGetValue(product.catalogListingId, out var storeSpecificId))
                {
                    product.storeSpecificId = storeSpecificId;
                }
            }
        }
    }
}
