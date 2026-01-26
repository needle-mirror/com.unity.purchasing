namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents an App Store storefront, containing information about the user's App Store region.
    /// </summary>
    public class AppleStorefront
    {
        /// <summary>
        /// The unique identifier of the storefront.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The three-letter code representing the country or region associated with the storefront.
        /// </summary>
        public string CountryCode { get; }

        internal AppleStorefront(string id, string countryCode)
        {
            Id = id;
            CountryCode = countryCode;
        }
    }
}
