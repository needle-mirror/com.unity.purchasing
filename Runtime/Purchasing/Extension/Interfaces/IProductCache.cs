#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Extension
{
    // Interface for a cache of products specific to the store.
    interface IProductCache : IReadOnlyProductCache
    {
        /// <summary>
        /// Set an optional reverse-lookup service used by <see cref="ResolveByStoreSpecificIdAsync"/> and <see cref="FindOrResolveAsync"/>
        /// to resolve a native store id to a Unity uSku via the backend store-overrides endpoint.
        /// Wired only on Apple and Google stores; other stores leave it null.
        /// </summary>
        void SetReverseLookupService(IStoreOverrideReverseLookupService service);

        /// <summary>
        /// Asks the configured reverse-lookup service for the Unity uSku (+ resolved
        /// <see cref="ProductType"/>) that maps to <paramref name="storeSpecificId"/>. Returns null
        /// when no resolver is configured, the backend has no mapping, or the project has no
        /// remote catalog. Callers that want a synthesized Product on cache miss should use
        /// <see cref="FindOrResolveAsync"/>.
        /// </summary>
        Task<ResolvedUSku?> ResolveByStoreSpecificIdAsync(string? storeSpecificId);

        /// <summary>
        /// Primary lookup used from purchase-callback hot paths. Tries, in order:
        /// <list type="number">
        /// <item>Sync <see cref="Find"/> against the local cache (matches both uSku and storeSpecificId indexes).</item>
        /// <item>Awaits the backend reverse-lookup to translate <paramref name="storeSpecificId"/> to a uSku, then re-runs Find for that uSku.</item>
        /// <item>Falls back to <see cref="Product.CreateUnknownProduct(string)"/> using the backend-resolved uSku when available, otherwise the original <paramref name="storeSpecificId"/>.</item>
        /// </list>
        /// Exceptions from the resolver are swallowed and the sync fallback is returned — these
        /// callers must never propagate an exception up the store callback.
        /// </summary>
        Task<Product> FindOrResolveAsync(string? storeSpecificId);

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
