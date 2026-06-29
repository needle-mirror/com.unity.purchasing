namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A single purchasable listing for a <see cref="Product"/>. A Product may carry multiple
    /// listings to offer various purchase options for the same product.
    /// </summary>
    public class CatalogListing
    {
        internal CatalogListing(string catalogListingId, ProductDefinition definition, ProductMetadata metadata, bool availableToPurchase)
        {
            this.id = catalogListingId;
            this.definition = definition;
            this.metadata = metadata;
            this.availableToPurchase = availableToPurchase;
        }

        /// <summary>
        /// The catalog listing id.
        /// This identifier is used as the key for catalog listings.
        /// </summary>
        public string id { get; }

        /// <summary>
        /// Store-side product definition (id, storeSpecificId, type, payouts) for this listing.
        /// </summary>
        public ProductDefinition definition { get; internal set; }

        /// <summary>
        /// Localized metadata (title, description, price, currency) for this listing.
        /// </summary>
        public ProductMetadata metadata { get; internal set; }

        /// <summary>
        /// Whether this listing is currently available to purchase from the store subsystem.
        /// </summary>
        public bool availableToPurchase { get; internal set; }
    }
}
