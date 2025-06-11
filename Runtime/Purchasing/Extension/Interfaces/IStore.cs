#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Represents the interface of the underlying store system such as Google Play,
    /// or the Apple App store.
    /// </summary>
    interface IStore
    {
        /// <summary>
        /// Initialize the connection to the store,
        /// asynchronously with results returned via <see cref="IStoreConnectCallback"/>.
        /// </summary>
        void Connect();

        /// <summary>
        /// Fetch the latest product metadata, including purchase receipts,
        /// asynchronously with results returned via <see cref="IStorePurchaseFetchCallback"/>.
        /// </summary>
        /// <param name="products"> The collection of products desired </param>
        void FetchProducts(IReadOnlyCollection<ProductDefinition> products);

        /// <summary>
        /// Fetch previously existing purchases.
        /// </summary>
        void FetchPurchases();

        /// <summary>
        /// Handle a purchase request from a user.
        /// </summary>
        /// <param name="cart"> The cart to be purchased. </param>
        void Purchase(ICart cart);

        /// <summary>
        /// Called by Unity Purchasing when a transaction has been recorded.
        /// Store systems should perform any housekeeping here,
        /// such as closing transactions or consuming consumables.
        /// </summary>
        /// <param name="pendingOrder"> The order to be finished </param>
        void FinishTransaction(PendingOrder pendingOrder);

        /// <summary>
        /// Checks if the Product in question has been purchased.
        /// OnCheckEntitlementSucceeded will be called when successful with the entitlement status.
        ///
        /// Google Play Store: The result is returned by Google.
        /// Apple Store: The result is determined locally by cached orders.
        /// </summary>
        /// <param name="product">The Product to check for Entitlement.</param>
        /// <returns>Whether the product is entitled or not.</returns>
        void CheckEntitlement(ProductDefinition product);

        /// <summary>
        /// Add an additional callback for FetchProducts calls
        /// </summary>
        /// <param name="productsCallback">Implementation of the products Callback Interface</param>
        void SetProductsCallback(IStoreProductsCallback productsCallback);

        /// <summary>
        /// Add an additional fetch Callback for FetchPurchases calls
        /// </summary>
        /// <param name="fetchPurchaseCallback">Implementation of the fetch purchase Callback Interface</param>
        void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchPurchaseCallback);

        /// <summary>
        /// Add an additional Callback for Purchases calls
        /// </summary>
        /// <param name="purchaseCallback">Implementation of the purchase Callback Interface</param>
        void SetPurchaseCallback(IStorePurchaseCallback purchaseCallback);

        /// <summary>
        /// Add an additional confirm Callback for ConfirmPurchase calls
        /// </summary>
        /// <param name="confirmCallback">Implementation of the order confirm Callback Interface</param>
        void SetPurchaseConfirmCallback(IStorePurchaseConfirmCallback confirmCallback);

        /// <summary>
        /// Add a callback for Connect calls
        /// </summary>
        /// <param name="storeConnectCallback"> Implementation of the connect Callback Interface</param>
        void SetStoreConnectionCallback(IStoreConnectCallback storeConnectCallback);

        /// <summary>
        /// Add an additional entitlement Callback for CheckEntitlment calls
        /// </summary>
        /// <param name="entitlementCallback">Implementation of the order confirm Callback Interface</param>
        void SetEntitlementCheckCallback(IStoreCheckEntitlementCallback entitlementCallback);

        /// <summary>
        /// Add an additional entitlement revoked Callback for OnEntitlementRevokedCallback calls
        /// </summary>
        /// <param name="entitlementRevokedCallback">Implementation of the OnEntitlementRevokedCallback Interface</param>
        void SetOnRevokedEntitlementCallback(IOnEntitlementRevokedCallback entitlementRevokedCallback);
    }
}
