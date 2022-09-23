namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Product definition used by Apple App Store.
    /// This is a representation of SKProduct
    /// <a href="https://developer.apple.com/documentation/storekit/skproduct">Apple documentation</a>
    /// </summary>
    public class AppleProductMetadata : ProductMetadata
    {
        /// <summary>
        /// A Boolean value that indicates whether the product is available for family sharing in App Store Connect.
        /// </summary>
        public bool isFamilyShareable { get; }

        //With objective-C BOOL to json limitations the value for the [isFamilyShareable] key will be "true" or "false"
        internal AppleProductMetadata(ProductMetadata baseProductMetadata, string isFamilyShareable)
            : base(baseProductMetadata)
        {
            this.isFamilyShareable = isFamilyShareable == "true";
        }

        //With objective-C BOOL to json limitations the value for the [isFamilyShareable] key will be "true" or "false"
        internal AppleProductMetadata(string priceString, string title, string description, string currencyCode, decimal localizedPrice, string isFamilyShareable)
            : base(priceString, title, description, currencyCode, localizedPrice)
        {
            this.isFamilyShareable = isFamilyShareable == "true";
        }
    }
}
