namespace UnityEngine.Purchasing.Default {
    /// <summary>
    /// A common format for Billing Subsystems to use to
    /// describe available In App Purchases to the Biller,
    /// including purchase state via Receipt and Transaction
    /// Identifiers.
    /// </summary>
    public class WinProductDescription {
        public string platformSpecificID { get; private set; }
        public string price { get; private set; }
        public string title { get; private set; }
        public string description { get; private set; }
        public string ISOCurrencyCode { get; private set; }
        public decimal priceDecimal { get; private set; }
        public string receipt { get; private set; }
        public string transactionID { get; private set; }
        public bool consumable { get; private set; }

        public WinProductDescription (string id, string price, string title, string description,
                                   string isoCode, decimal priceD, string receipt = null, string transactionId = null, bool consumable = false) {
            platformSpecificID = id;
            this.price = price;
            this.title = title;
            this.description = description;
            this.ISOCurrencyCode = isoCode;
            this.priceDecimal = priceD;
            this.receipt = receipt;
            this.transactionID = transactionId;
            this.consumable = consumable;
        }
    }
}
