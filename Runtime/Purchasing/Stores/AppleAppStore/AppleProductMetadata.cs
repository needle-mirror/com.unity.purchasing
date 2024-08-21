namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Product definition used by Apple App Store.
    /// This is a representation of Product
    /// <a href="https://developer.apple.com/documentation/storekit/product">Apple documentation</a>
    /// </summary>
    public class AppleProductMetadata : ProductMetadata
    {
        /// <summary>
        /// A Boolean value that indicates whether the product is available for family sharing in App Store Connect.
        /// </summary>
        public bool isFamilyShareable { get; }

        internal AppleProductMetadata(ProductMetadata baseProductMetadata, bool isFamilyShareable)
            : base(baseProductMetadata)
        {
            this.isFamilyShareable = isFamilyShareable;
        }

        internal AppleProductMetadata(string priceString, string title, string description, string currencyCode, decimal localizedPrice, bool isFamilyShareable)
            : base(priceString, title, description, currencyCode, localizedPrice)
        {
            this.isFamilyShareable = isFamilyShareable;
        }
    }
}
