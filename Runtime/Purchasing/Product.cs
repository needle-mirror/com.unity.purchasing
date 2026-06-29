using System;
using System.Collections.Generic;
using System.Linq;
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

        internal Product(ProductDefinition definition, ProductMetadata metadata, bool availableToPurchase = false)
        {
            m_CatalogListings = new Dictionary<string, CatalogListing>();
            if (definition != null)
            {
                var listing = new CatalogListing(definition.catalogListingId, definition, metadata, availableToPurchase);
                uSku = definition.id;
                type = definition.type;
                m_CatalogListings[definition.catalogListingId] = listing;
                m_BaseListing = listing;
            }
            else
            {
                // For test purposes only, definition should never be null for a real Product
                uSku = null;
                type = ProductType.Unknown;
            }

            catalogListings = m_CatalogListings;
        }

        readonly Dictionary<string, CatalogListing> m_CatalogListings;
        CatalogListing m_BaseListing;

        /// <summary>
        /// Attach a new catalog listing to this product. The listing is keyed by its
        /// <see cref="CatalogListing.id"/> and shows up in <see cref="catalogListings"/>.
        /// </summary>
        internal void AddCatalogListing(CatalogListing listing)
        {
            if (listing?.id == null)
            {
                return;
            }
            m_CatalogListings[listing.id] = listing;
            if (uSku != null && listing.definition?.id == uSku)
            {
                m_BaseListing = listing;
            }
        }

        internal static Product CreateUnknownProduct(string productId)
        {
            return new Product(new ProductDefinition(productId, ProductType.Unknown), new ProductMetadata());
        }

        /// <summary>
        /// The Unity-side identifier for this product. Equivalent to the product id authored in the
        /// Unity catalog; distinct from any store-specific id, which lives on each
        /// <see cref="CatalogListing.definition"/>.
        /// </summary>
        public string uSku { get; }

        /// <summary>
        /// The product type. Mirrors the <see cref="ProductDefinition.type"/> of the listing
        /// the product was constructed from.
        /// </summary>
        public ProductType type { get; }

        /// <summary>
        /// All catalog listings attached to this product, keyed by <see cref="CatalogListing.id"/>.
        /// Contains a single entry (the base listing, where <c>id == <see cref="uSku"/></c>) when the
        /// product was constructed from a <see cref="ProductDefinition"/>.
        /// Look up a specific listing with <c>product.catalogListings[catalogListingId]</c>.
        /// </summary>
        public IReadOnlyDictionary<string, CatalogListing> catalogListings { get; }

        /// <summary>
        /// The base listing for this product — the listing whose <see cref="CatalogListing.definition"/>
        /// id equals <see cref="uSku"/>. Cached at construction and refreshed by
        /// <see cref="AddCatalogListing"/> when a newly attached listing matches uSku.
        /// </summary>
        internal CatalogListing baseListing => m_BaseListing;

        /// <summary>
        /// Basic immutable product properties.
        /// </summary>
        public ProductDefinition definition => baseListing?.definition;

        /// <summary>
        /// Localized metadata provided by the store system.
        /// </summary>
        /// <value>The metadata.</value>
        public ProductMetadata metadata
        {
            get => baseListing?.metadata;
            internal set
            {
                if (baseListing != null)
                {
                    baseListing.metadata = value;
                }
            }
        }

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
        public bool availableToPurchase
        {
            get => baseListing?.availableToPurchase ?? false;
            internal set
            {
                if (baseListing != null)
                {
                    baseListing.availableToPurchase = value;
                }
            }
        }

        /// <summary>
        /// A unique identifier for this product's transaction.
        /// This will only be set when the product was purchased during this session.
        /// Consumable's transactionID are not set between app restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `transactionID` is removed.
        /// </summary>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        public string transactionID { get; internal set; }

        /// <summary>
        /// A unique identifier for this Apple product's original transaction.
        ///
        /// This will only be set when the Apple product was purchased during this session.
        /// </summary>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        public string appleOriginalTransactionID { get; internal set; }

        /// <summary>
        /// Indicates if this Apple product is restored.
        /// </summary>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        public bool appleProductIsRestored { get; internal set; }

        /// <summary>
        /// Owned Non Consumables and Subscriptions should always have receipts.
        /// Consumable's receipts are not persisted between App restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `receipt` is removed.
        /// </summary>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
        public bool hasReceipt => !string.IsNullOrEmpty(transactionID) && !string.IsNullOrEmpty(receipt);

        /// <summary>
        /// The purchase receipt for this product, if owned.
        /// For consumable purchases, this will be the most recent purchase receipt.
        /// Consumable's receipts are not set between app restarts unless it has a pending transaction.
        /// Once a consumable has been acknowledged (ConfirmPendingPurchase) the `receipt` is removed.
        /// Receipts is in JSON format.
        /// </summary>
        [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
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
// Obsolete: Product.transactionID, IAppleStoreExtendedPurchaseService.appReceipt
#pragma warning disable 618, 612
                if (transactionID == null)
                {
                    return null;
                }
                var curReceipt = UnityIAPServices.DefaultPurchase().Apple?.appReceipt;
                return CreateUnifiedReceipt(curReceipt, transactionID, defaultStore == AppStore.AppleAppStore ? AppleAppStore.Name : MacAppStore.Name);
#pragma warning restore 618, 612
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

            return uSku == p.uSku;
        }

        /// <summary>
        /// Get the unique Hash representing the product.
        /// </summary>
        /// <returns> The hash code as integer </returns>
        public override int GetHashCode()
        {
            return uSku?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Returns a string representation of the product.
        /// </summary>
        /// <returns> A string representation of the product.</returns>
        public override string ToString()
        {
            var listings = string.Join(", ", m_CatalogListings.Values
                .Select(l => $"{{id: {l.id}, definition: {l.definition}, metadata: {l.metadata}, availableToPurchase: {l.availableToPurchase}}}"));
// Obsolete: Product.receipt
#pragma warning disable 618, 612
            return $"Product: uSku={uSku}, type={type}, catalogListings=[{listings}], receipt={receipt}";
#pragma warning restore 618, 612
        }
    }
}
