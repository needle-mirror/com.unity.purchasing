#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    // Interface for a cache of products specific to the store.
    interface IProductCache : IReadOnlyProductCache
    {
        // Keyed by Product.uSku — the Unity-side product identifier.
        Dictionary<string, Product> productsByUSku { get; }
        // Keyed by CatalogListing.id — one entry per listing (multiple per product when a product has multiple listings).
        Dictionary<string, Product> productsByCatalogListingId { get; }
        void Add(Product product);
        void Add(List<Product> product);
        void Remove(Product product);
        /// <summary>
        /// Removes all cached products and catalog listings. Used when the underlying
        /// catalog is no longer valid for the current session (e.g. the authenticated
        /// end-user account has changed and the segmented catalog must be refetched).
        /// </summary>
        void Clear();
        /// <summary>
        /// Attach the given catalog listing to the product matching <paramref name="uSku"/> and
        /// re-index the cache so the listing is reachable by its <see cref="CatalogListing.id"/>
        /// and by its store-specific id. Returns the updated product, or <c>null</c> if no
        /// product with that uSku is currently cached or the listing collides with a different
        /// product already in the cache.
        /// </summary>
        Product? AddCatalogListing(string? uSku, CatalogListing listing);
        /// <summary>
        /// Gets the specific catalog listing (on any cached product) whose
        /// <c>definition.storeSpecificId</c> matches <paramref name="storeSpecificId"/>.
        /// <para>
        /// TEMPORARY: relies on the current invariant that each storeSpecificId maps to a single
        /// catalog listing. If that uniqueness ever changes, callers may need an explicit
        /// disambiguator (e.g. the owning product) instead. Kept on the internal
        /// <see cref="IProductCache"/> rather than the public <see cref="IReadOnlyProductCache"/>
        /// so we can revisit/remove it without a breaking API change.
        /// </para>
        /// </summary>
        /// <param name="storeSpecificId">The <c>ProductDefinition.storeSpecificId</c> to query by.</param>
        /// <returns>The matching catalog listing if found, otherwise returns null.</returns>
        CatalogListing? FindCatalogListingByStoreSpecificId(string? storeSpecificId);
    }
}
