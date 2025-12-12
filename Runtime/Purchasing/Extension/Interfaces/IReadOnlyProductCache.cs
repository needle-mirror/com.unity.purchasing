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
        /// Gets a product by its product ID.
        /// </summary>
        /// <param name="productId">Product ID to query by.</param>
        /// <returns>The matching product if found, otherwise returns null.</returns>
        Product? Find(string? productId);
        /// <summary>
        /// Find a matching product by its ID.
        /// </summary>
        /// <param name="productId">Product ID to query by.</param>
        /// <returns>The matching product if found, otherwise returns an Unknown Product.</returns>
        Product FindOrDefault(string? productId);
    }
}
