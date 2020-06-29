namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// A common format for store subsystems to use to
    /// describe available In App Purchases to UnityPurchasing,
    /// including purchase state via Receipt and Transaction
    /// Identifiers.
    /// </summary>
    public class ProductDescription
    {
        public ProductDescription(string id, ProductMetadata metadata,
                                  string receipt, string transactionId)
        {
            storeSpecificId = id;
            this.metadata = metadata;
            this.receipt = receipt;
            this.transactionId = transactionId;
        }

        public ProductDescription(string id, ProductMetadata metadata,
                                  string receipt, string transactionId, ProductType type)
            : this(id, metadata, receipt, transactionId)
        {
            this.type = type;
        }

        public ProductDescription(string id, ProductMetadata metadata) : this(id, metadata, null, null)
        {
        }

        public string storeSpecificId { get; private set; }

        /// <summary>
        /// If this ProductDescription was explicitly queried by Unity IAP
        /// then it is not necessary to specify a type since it is already
        /// known from the product definition.
        ///
        /// Otherwise, if this ProductDescription is unknown, type must
        /// be correctly so the product can be handled correctly.
        /// </summary>
        public ProductType type;
        public ProductMetadata metadata { get; private set; }
        public string receipt { get; private set; }
        public string transactionId { get; set; }
    }
}
