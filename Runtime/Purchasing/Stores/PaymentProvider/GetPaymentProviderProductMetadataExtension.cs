namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Extension class to ProductMetadata to add a method to retrieve the Payment Provider Product Metadata.
    /// </summary>
    public static class GetPaymentProviderProductMetadataExtension
    {
        /// <summary>
        /// Get the Payment Provider Product Metadata. Can be null.
        /// </summary>
        /// <param name="productMetadata">Product Metadata</param>
        /// <returns>Payment Provider Product Metadata or null if the current store is not the Payment Provider store.</returns>
        public static PaymentProviderProductMetadata GetPaymentProviderProductMetadata(this ProductMetadata productMetadata)
        {
            return productMetadata as PaymentProviderProductMetadata;
        }
    }
}
