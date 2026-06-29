#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Product metadata returned by the Payment Provider store.
    /// </summary>
    public class PaymentProviderProductMetadata : ProductMetadata
    {
        /// <summary>
        /// The localized webshop price for this product, when one has been configured
        /// on the catalog listing. Null when no webshop price is set.
        /// </summary>
        public decimal? localizedWebshopPrice { get; internal set; }

        /// <summary>
        /// The localized webshop price formatted with currency symbol.
        /// Null string when no webshop price is set.
        /// </summary>
        public string? webshopPriceString { get; internal set; }

        /// <summary>
        /// True when the item has a corresponding Webshop item.
        /// </summary>
        public bool hasWebshop { get; internal set; }

        internal PaymentProviderProductMetadata(
            string priceString,
            string title,
            string description,
            string? currencyCode,
            decimal localizedPrice,
            decimal? localizedWebshopPrice,
            string? webshopPriceString,
            bool hasWebshop)
            : base(priceString, title, description, currencyCode, localizedPrice)
        {
            this.localizedWebshopPrice = localizedWebshopPrice;
            this.webshopPriceString = webshopPriceString;
            this.hasWebshop = hasWebshop;
        }
    }
}
