using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// May be purchased as an In-App Purchase.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Products must have a definition as minimum.
        ///
        /// Further metadata may be populated following retrieval from the
        /// store system.
        /// </summary>
        [Obsolete("This constructor is obsolete and should not be used. Use the Product(ProductDefinition, ProductMetadata) constructor and assign the receipt separately if needed.")]
        internal Product(ProductDefinition definition, ProductMetadata metadata, string receipt) : this(definition, metadata)
        {
            this.receipt = receipt;
        }

        internal Product(ProductDefinition definition, ProductMetadata metadata)
        {
            this.definition = definition;
            this.metadata = metadata;
        }

        internal static Product CreateUnknownProduct(string productId)
        {
            return new Product(new ProductDefinition(productId, ProductType.Unknown), new ProductMetadata());
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
        /// This will only be set when the product was purchased during this session.
        /// Consumable's transactionID are not set between app restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `transactionID` is removed.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public string transactionID { get; internal set; }

        /// <summary>
        /// A unique identifier for this Apple product's original transaction.
        ///
        /// This will only be set when the Apple product was purchased during this session.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public string appleOriginalTransactionID { get; internal set; }

        /// <summary>
        /// Indicates if this Apple product is restored.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public bool appleProductIsRestored { get; internal set; }

        /// <summary>
        /// Owned Non Consumables and Subscriptions should always have receipts.
        /// Consumable's receipts are not persisted between App restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `receipt` is removed.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public bool hasReceipt => !string.IsNullOrEmpty(transactionID) && !string.IsNullOrEmpty(receipt);

        /// <summary>
        /// The purchase receipt for this product, if owned.
        /// For consumable purchases, this will be the most recent purchase receipt.
        /// Consumable's receipts are not set between app restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `receipt` is removed.
        /// Receipts is in JSON format.
        /// </summary>
        [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
        public string receipt
        {
            get => GetReceipt();
            internal set => SetReceipt(value);
        }

        string GetReceipt()
        {
            var defaultStore = DefaultStoreHelper.GetDefaultBuiltInAppStore();
            if (defaultStore == AppStore.AppleAppStore || defaultStore == AppStore.MacAppStore)
            {
                if (transactionID == null)
                {
                    return null;
                }
                var curReceipt = UnityIAPServices.DefaultPurchase().Apple?.appReceipt;
                return CreateUnifiedReceipt(curReceipt, transactionID, defaultStore == AppStore.AppleAppStore ? AppleAppStore.Name : MacAppStore.Name);
            }

            return m_Receipt;
        }


        static string CreateUnifiedReceipt(string rawReceipt, string transactionId, string storeName)
        {
            return UnifiedReceiptFormatter.FormatUnifiedReceipt(rawReceipt, transactionId, storeName);
        }

        void SetReceipt(string curReceipt)
        {
            var defaultStore = DefaultStoreHelper.GetDefaultBuiltInAppStore();
            if (defaultStore != AppStore.AppleAppStore && defaultStore != AppStore.MacAppStore)
            {
                m_Receipt = curReceipt;
            }
        }

        string m_Receipt;

        /// <summary>
        /// Check if this product is equal to another.
        /// </summary>
        /// <param name="obj"> The product to compare with this object. </param>
        /// <returns> True if the products are equal </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var p = obj as Product;
            if (p == null)
            {
                return false;
            }

            return definition.Equals(p.definition);
        }

        /// <summary>
        /// Get the unique Hash representing the product.
        /// </summary>
        /// <returns> The hash code as integer </returns>
        public override int GetHashCode()
        {
            return definition.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the product.
        /// </summary>
        /// <returns> A string representation of the product.</returns>
        public override string ToString()
        {
            return $"Product: {definition}, {metadata}, {receipt}";
        }
    }
}
