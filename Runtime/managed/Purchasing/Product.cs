namespace UnityEngine.Purchasing
{
    /// <summary>
    /// May be purchased as an In App Purchase.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Products must have a definition as minimum.
        ///
        /// Further metadata may be populated following retrieval from the
        /// store system.
        /// </summary>
        internal Product(ProductDefinition definition, ProductMetadata metadata, string receipt)
        {
            this.definition = definition;
            this.metadata = metadata;
            this.receipt = receipt;
        }

        internal Product(ProductDefinition definition, ProductMetadata metadata) : this(definition, metadata, null)
        {
        }

        /// <summary>
        /// Basic immutable product properties.
        /// </summary>
        public ProductDefinition definition { get; private set; }

        /// <summary>
        /// Localized metadata provided by the store system.
        /// </summary>
        /// <value>The metadata.</value>
        public ProductMetadata metadata { get; internal set; }

        /// <summary>
        /// Determine if this product is available to purchase according to
        /// the store subsystem.
        ///
        /// This will be false if the product's identifier is unknown,
        /// incorrect or otherwise disabled with the store provider
        /// (ie Apple, Google et al).
        ///
        /// If this is false, purchase attempts will immediately fail.
        /// </summary>
        public bool availableToPurchase { get; internal set; }

        /// <summary>
        /// A unique identifier for this product's transaction.
        ///
        /// This will only be set when the product was purchased during this session.
        /// </summary>
        public string transactionID { get; internal set; }

        /// <summary>
        /// Owned Non Consumables and Subscriptions should always have receipts.
        /// Consumable's receipts are not persisted between App restarts.
        /// </summary>
        public bool hasReceipt
        {
            get { return !string.IsNullOrEmpty(receipt); }
        }

        /// <summary>
        /// The purchase receipt for this product, if owned.
        /// For consumable purchases, this will be the most recent purchase receipt.
        /// Consumable receipts are not saved between app restarts.
        /// Receipts is in JSON format.
        /// </summary>
        public string receipt { get; internal set; }

        /// <summary>
        /// Products may be contained in a Set.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            Product p = obj as Product;
            if (p == null)
                return false;

            return (definition.Equals(p.definition));
        }

        public override int GetHashCode()
        {
            return definition.GetHashCode();
        }
    }
}
