#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Extension class to ProductMetadata to add a method to retrieve the Apple Product Metadata
    /// </summary>
    public static class GetAppleProductMetadataExtension
    {
        /// <summary>
        /// Get the Apple Product Metadata. Can be null.
        /// </summary>
        /// <param name="productMetadata">Product Metadata</param>
        /// <returns>Apple Product Metadata or null if the current store is not the Apple store.</returns>
        public static AppleProductMetadata? GetAppleProductMetadata(this ProductMetadata productMetadata)
        {
            return productMetadata as AppleProductMetadata;
        }
    }
}
