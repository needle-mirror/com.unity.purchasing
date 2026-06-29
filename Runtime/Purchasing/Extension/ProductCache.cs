#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityEngine.Purchasing.Extension
{
    class ProductCache : IProductCache
    {
        ObservableCollection<Product> m_Products = new();
        readonly ReadOnlyObservableCollection<Product> m_ProductsReadOnly;

        // Keyed by Product.uSku — the Unity-side product identifier.
        public Dictionary<string, Product> productsByUSku { get; } = new();
        // Keyed by CatalogListing.id — one entry per listing (multiple per product when a product has multiple listings).
        public Dictionary<string, Product> productsByCatalogListingId { get; } = new();
        // Keyed by ProductDefinition.storeSpecificId — the store-side identifier (Apple/Google sku, etc.).
        Dictionary<string, Product> productsByStoreSpecificId { get; } = new();
        // Same key as productsByStoreSpecificId, but resolves to the specific CatalogListing
        // (not the owning Product). Lets receipt/purchase paths get the matched listing in O(1).
        Dictionary<string, CatalogListing> catalogListingsByStoreSpecificId { get; } = new();

        internal ProductCache()
        {
            m_ProductsReadOnly = new ReadOnlyObservableCollection<Product>(m_Products);
        }

        public void Add(List<Product> products)
        {
            foreach (var product in products)
            {
                Add(product);
            }
        }

        public void Add(Product product)
        {
            // Reject the Add only if a *different* product instance already owns any of the keys we're
            // about to index. Re-adding the same product instance is allowed so callers can
            // index new catalog listings that were attached after the first Add.
            // Use reference inequality (!=) — Product.Equals is overridden to compare by uSku, so
            // Equals() would treat two distinct Product instances with the same uSku as equal and
            // silently let the Add through.
            // Use the exact uSku index here, not the polymorphic Find — otherwise a new product
            // whose uSku happens to equal another cached product's catalogListingId or
            // storeSpecificSku would be falsely rejected.
            var existingByUSku = product.uSku != null && productsByUSku.TryGetValue(product.uSku, out var byUSku) ? byUSku : null;
            if (existingByUSku != null && existingByUSku != product)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"ProductCache: cannot add product with uSku '{product.uSku}'; that uSku is already used by another cached product (uSku '{existingByUSku.uSku}').");
                return;
            }
            foreach (var listing in product.catalogListings.Values)
            {
                var existingByListing = FindByCatalogListingId(listing.id);
                if (existingByListing != null && existingByListing != product)
                {
                    Debug.unityLogger.LogIAPWarning(
                        $"ProductCache: cannot add product with uSku '{product.uSku}'; catalog listing id '{listing.id}' is already used by another cached product (uSku '{existingByListing.uSku}').");
                    return;
                }
                var existingByStore = FindByStoreSpecificId(listing.definition?.storeSpecificId);
                if (existingByStore != null && existingByStore != product)
                {
                    Debug.unityLogger.LogIAPWarning(
                        $"ProductCache: cannot add product with uSku '{product.uSku}'; storeSpecificSku '{listing.definition?.storeSpecificId}' is already used by another cached product (uSku '{existingByStore.uSku}').");
                    return;
                }
            }

            if (product.uSku != null)
            {
                productsByUSku[product.uSku] = product;
            }
            foreach (var listing in product.catalogListings.Values)
            {
                if (listing.id != null)
                {
                    productsByCatalogListingId[listing.id] = product;
                }
                if (listing.definition?.storeSpecificId != null)
                {
                    productsByStoreSpecificId[listing.definition.storeSpecificId] = product;
                    catalogListingsByStoreSpecificId[listing.definition.storeSpecificId] = listing;
                }
            }
            if (!m_Products.Contains(product))
            {
                m_Products.Add(product);
            }
        }

        public ReadOnlyObservableCollection<Product> GetProducts()
        {
            return m_ProductsReadOnly;
        }

        public void Clear()
        {
            productsByUSku.Clear();
            productsByCatalogListingId.Clear();
            productsByStoreSpecificId.Clear();
            catalogListingsByStoreSpecificId.Clear();
            m_Products.Clear();
        }

        public void Remove(Product product)
        {
            if (product.uSku != null)
            {
                productsByUSku.Remove(product.uSku);
            }
            foreach (var listing in product.catalogListings.Values)
            {
                if (listing.id != null)
                {
                    productsByCatalogListingId.Remove(listing.id);
                }
                if (listing.definition?.storeSpecificId != null)
                {
                    productsByStoreSpecificId.Remove(listing.definition.storeSpecificId);
                    catalogListingsByStoreSpecificId.Remove(listing.definition.storeSpecificId);
                }
            }
            m_Products.Remove(product);
        }

        public Product? AddCatalogListing(string? uSku, CatalogListing listing)
        {
            if (uSku == null || !productsByUSku.TryGetValue(uSku, out var product))
            {
                return null;
            }

            // Reject if the listing's id or its storeSpecificId is already taken by a *different* product.
            // Re-attaching to the same product is allowed so callers can refresh a listing's metadata.
            var existingByListing = FindByCatalogListingId(listing?.id);
            if (existingByListing != null && existingByListing != product)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"ProductCache: cannot attach catalog listing '{listing?.id}' to product '{uSku}'; that catalog listing id is already used by another cached product (uSku '{existingByListing.uSku}').");
                return null;
            }
            var existingByStore = FindByStoreSpecificId(listing?.definition?.storeSpecificId);
            if (existingByStore != null && existingByStore != product)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"ProductCache: cannot attach catalog listing '{listing?.id}' to product '{uSku}'; storeSpecificSku '{listing?.definition?.storeSpecificId}' is already used by another cached product (uSku '{existingByStore.uSku}').");
                return null;
            }

            product.AddCatalogListing(listing);

            if (listing?.id != null)
            {
                productsByCatalogListingId[listing.id] = product;
            }
            if (listing?.definition?.storeSpecificId != null)
            {
                productsByStoreSpecificId[listing.definition.storeSpecificId] = product;
                catalogListingsByStoreSpecificId[listing.definition.storeSpecificId] = listing;
            }
            return product;
        }

        public Product FindOrDefault(string? uSku)
        {
            return Find(uSku) ?? Product.CreateUnknownProduct(uSku);
        }

        public Product? Find(string? uSku)
        {
            if (uSku == null)
            {
                return null;
            }
            if (productsByUSku.TryGetValue(uSku, out var byUSku))
            {
                return byUSku;
            }
            return FindByStoreSpecificId(uSku);
        }

        public Product? FindByCatalogListingId(string? catalogListingId)
        {
            return catalogListingId != null && productsByCatalogListingId.TryGetValue(catalogListingId, out var product) ? product : null;
        }

        public Product? FindByStoreSpecificId(string? storeSpecificId)
        {
            return storeSpecificId != null && productsByStoreSpecificId.TryGetValue(storeSpecificId, out var product) ? product : null;
        }

        public CatalogListing? FindCatalogListingByStoreSpecificId(string? storeSpecificId)
        {
            return storeSpecificId != null && catalogListingsByStoreSpecificId.TryGetValue(storeSpecificId, out var listing) ? listing : null;
        }
    }
}
