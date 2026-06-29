#nullable enable
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Read only access to a product cache.
    /// </summary>
    public interface IReadOnlyProductCache
    {
        /// <summary>
        /// Get all products currently in the cache.
        /// </summary>
        ReadOnlyObservableCollection<Product> GetProducts();
        /// <summary>
        /// Gets a product by its <see cref="Product.uSku"/>.
        /// </summary>
        /// <param name="uSku">The product's <c>uSku</c>.</param>
        /// <returns>The matching product if found, otherwise returns null.</returns>
        Product? Find(string? uSku);
        /// <summary>
        /// Gets a product by its <see cref="Product.uSku"/>. Returns an unknown product when no match is found.
        /// </summary>
        /// <param name="uSku">The product's <c>uSku</c>.</param>
        /// <returns>The matching product if found, otherwise returns an Unknown Product.</returns>
        Product FindOrDefault(string? uSku);
        /// <summary>
        /// Gets the product that owns the catalog listing with the given id.
        /// </summary>
        /// <param name="catalogListingId">The <see cref="CatalogListing.id"/> to query by.</param>
        /// <returns>The owning product if found, otherwise returns null.</returns>
        Product? FindByCatalogListingId(string? catalogListingId);
        /// <summary>
        /// Gets the product that owns a catalog listing with the given store-specific id.
        /// </summary>
        /// <param name="storeSpecificId">The <c>ProductDefinition.storeSpecificId</c> to query by.</param>
        /// <returns>The owning product if found, otherwise returns null.</returns>
        Product? FindByStoreSpecificId(string? storeSpecificId);
    }
}
