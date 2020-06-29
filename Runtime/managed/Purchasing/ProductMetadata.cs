namespace UnityEngine.Purchasing
{
    public class ProductMetadata
    {
        public ProductMetadata(string priceString, string title, string description, string currencyCode, decimal localizedPrice)
        {
            localizedPriceString = priceString;
            localizedTitle = title;
            localizedDescription = description;
            isoCurrencyCode = currencyCode;
            this.localizedPrice = localizedPrice;
        }

        public ProductMetadata()
        {
        }

        /// <summary>
        /// Gets the localized price.
        /// This is the price formatted with currency symbol.
        /// </summary>
        /// <value>The localized price string.</value>
        public string localizedPriceString { get; internal set; }

        /// <summary>
        /// Gets the localized title, as retrieved from the store subsystem;
        /// Apple, Google etc.
        /// </summary>
        public string localizedTitle { get; internal set; }

        /// <summary>
        /// Gets the localized description, as retrieved from the store subsystem;
        /// Apple, Google etc.
        /// </summary>
        public string localizedDescription { get; internal set; }

        /// <summary>
        /// The product's currency in ISO 4217 format eg GBP, USD etc.
        /// </summary>
        public string isoCurrencyCode { get; internal set; }

        /// <summary>
        /// The product's price, denominated in the currency
        /// indicated by <c>isoCurrencySymbol</c>.
        /// </summary>
        public decimal localizedPrice { get; internal set; }
    }
}
