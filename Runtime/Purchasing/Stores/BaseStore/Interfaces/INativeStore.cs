using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An interface to native underlying store systems. Provides a base for opaquely typed
    /// communication across a language-bridge upon which additional functionality can be composed.
    /// Is used by most public IStore implementations which themselves are owned by the purchasing
    /// core.
    /// </summary>
    public interface INativeStore
    {
        /// <summary>
        /// Initializes a connection to the store.
        /// </summary>
        void Connect();

        /// <summary>
        /// Call the Store to retrieve the store products. The `IStoreCallback` will be call with the retrieved products.
        /// </summary>
        /// <param name="json">The catalog of products to retrieve the store information from in JSON format.</param>
        void FetchProducts(string json);

        /// <summary>
        /// Call the Store to retrieve existing purchases.
        /// </summary>
        void FetchExistingPurchases();

        /// <summary>
        /// Call the Store to purchase a product. The `IStoreCallback` will be call when the purchase is successful.
        /// </summary>
        /// <param name="productJson">The product to buy in JSON format.</param>
        /// <param name="optionsJson">A string used by some stores to fight fraudulent transactions.</param>
        void Purchase(string productJson, string optionsJson);

        /// <summary>
        /// Call the Store to consume a product.
        /// </summary>
        /// <param name="productJSON">Product to consume in JSON format.</param>
        /// <param name="transactionID">The transaction id of the receipt to close.</param>
        void FinishTransaction(string productJSON, string transactionID);

        /// <summary>
        /// Checks if the Product in question has been purchased.
        /// For Consumable Products, this will only be true for those whose transactions are not completed.
        /// For Subscriptions, this will check that the subscription is still active, according to the store it is purchased from.
        /// </summary>
        /// <param name="productJSON">The Product to check for Entitlement, in JSON format.</param>
        /// <returns>Whether the product is entitled or not.</returns>
        bool CheckEntitlement(string productJSON);
    }

    delegate void UnityPurchasingCallback(IntPtr subjectPtr, IntPtr payloadPtr, int entitlementStatus);
    delegate bool StorefrontChangeCallback(string storefrontCountryCode, string storefrontId);
}
