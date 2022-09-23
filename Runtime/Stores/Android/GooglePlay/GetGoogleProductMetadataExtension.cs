#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Extension class to ProductMetadata to add a method to retrieve the Google Product Metadata
    /// </summary>
    public static class GetGoogleProductMetadataExtension
    {
        /// <summary>
        /// Get the Google Product Metadata. Can be null.
        /// </summary>
        /// <param name="productMetadata">Product Metadata</param>
        /// <returns>Google Product Metadata</returns>
        public static GoogleProductMetadata? GetGoogleProductMetadata(this ProductMetadata productMetadata)
        {
            return productMetadata as GoogleProductMetadata;
        }
    }
}
