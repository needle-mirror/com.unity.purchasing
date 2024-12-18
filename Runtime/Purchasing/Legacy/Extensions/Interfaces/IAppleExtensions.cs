using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access iOS specific functionality.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public interface IAppleExtensions : IStoreExtension
    {
        /// <summary>
        /// Fetch the latest App Receipt from Apple.
        /// This requires an Internet connection and will prompt the user for their credentials.
        /// </summary>
        /// <param name="successCallback">This action will be called when the refresh is successful. The receipt will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the refresh is in error. The error's details will be passed through.</param>
        void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback);

        /// <summary>
        /// Fetch the most recent iOS 6 style transaction receipt for the given product.
        /// This is necessary to validate Ask-to-buy purchases, which don't show up in the App Receipt.
        /// </summary>
        /// <param name="product">The product to fetch the receipt from.</param>
        /// <returns>Returns the receipt if the product has a receipt or an empty string.</returns>
        string GetTransactionReceiptForProduct(Product product);

        /// <summary>
        /// Initiate a request to Apple to restore previously made purchases.
        /// </summary>
        /// <param name="callback">Action will be called when the request to Apple comes back. The bool will be true if it was successful with a null string or false if it was not with the error message in the string.</param>
        void RestoreTransactions(Action<bool, string> callback);

        /// <summary>
        /// Called when a processing a purchase from Apple that is in the "onProductPurchaseDeferred" state.
        /// </summary>
        /// <param name="callback">Action will be called with the product that is in the "onProductPurchaseDeferred" state.</param>
        void RegisterPurchaseDeferredListener(Action<Product> callback);

        /// <summary>
        /// Modify payment request with "applicationUsername" for fraud detection.
        /// </summary>
        /// <param name="applicationUsername">The application Username for fraud detection.</param>
        void SetApplicationUsername(string applicationUsername);

        /// <summary>
        /// For testing purposes only.
        ///
        /// Modify payment request for testing ask-to-buy.
        /// </summary>
        bool simulateAskToBuy { get; set; }

        /// <summary>
        /// Returns the current promoted product order on the device
        /// </summary>
        /// <param name="successCallback">This action will be called when the fetch is successful. The list of products will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the fetch is in error.</param>
        void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback);

        /// <summary>
        /// Overrides the promoted product order on the device.
        /// </summary>
        /// <param name="products">The new order of promoted products for the device.</param>
        void SetStorePromotionOrder(List<Product> products);

        /// <summary>
        /// Returns the current promoted product order on the device
        /// </summary>
        /// <param name="product">Product to change visibility.</param>
        /// <param name="successCallback">This action will be called when the fetch is successful. The productId and visibility will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the fetch is in error.</param>
        void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback);

        /// <summary>
        /// Override the visibility of a product on the device.
        /// </summary>
        /// <param name="product">Product to change visibility.</param>
        /// <param name="visible">The new product visibility.</param>
        void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible);

        /// <summary>
        /// Call the `UnityEarlyTransactionObserver.initiateQueuedPayments`
        /// </summary>
        void ContinuePromotionalPurchases();

        /// <summary>
        /// Extracting Introductory Price subscription related product details.
        /// </summary>
        /// <returns>returns the Introductory Price subscription related product details or an empty dictionary</returns>
        Dictionary<string, string> GetIntroductoryPriceDictionary();

        /// <summary>
        /// Extracting product details.
        /// </summary>
        /// <returns>returns product details or an empty dictionary</returns>
        Dictionary<string, string> GetProductDetails();

        /// <summary>
        /// Initiate Apple Subscription Offer Code redemption API, presentCodeRedemptionSheet
        /// </summary>
        void PresentCodeRedemptionSheet();
    }
}
