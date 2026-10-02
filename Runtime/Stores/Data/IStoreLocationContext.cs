#nullable enable

namespace UnityEngine.Purchasing.Stores
{
    internal interface IStoreLocationContext
    {
        string? CountryCode { get; set; }
        string? CurrencyCode { get; set; }

        /// <summary>
        /// Country and currency read together, consistent with every write — reading the two
        /// properties separately can pair one storefront's country with another's currency.
        /// </summary>
        (string? CountryCode, string? CurrencyCode) Snapshot();

        /// <summary>
        /// Applies an App Store storefront: its ISO 3166-1 alpha-3 country and, on iOS 17+ /
        /// macOS 14+, its ISO 4217 currency. Both come from the same storefront, so they are
        /// set together; an unknown country sets neither.
        /// </summary>
        void SetFromStorefront(string? alpha3CountryCode, string? currencyCode);
    }
}
