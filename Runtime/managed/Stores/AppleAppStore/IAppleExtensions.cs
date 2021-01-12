using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access iOS specific functionality.
    /// </summary>
    public interface IAppleExtensions : IStoreExtension
    {
        /// <summary>
        /// Fetch the latest App Receipt from Apple.
        ///
        /// This requires an Internet connection and will prompt the user for their credentials.
        /// </summary>
        void RefreshAppReceipt(Action<string> successCallback, Action errorCallback);

        /// <summary>
        /// Fetch the most recent iOS 6 style transaction receipt for the given product.
        ///
        /// This is necessary to validate Ask-to-buy purchases, which don't show up in the
        /// App Receipt.
        /// </summary>
        string GetTransactionReceiptForProduct (Product product);

        void RestoreTransactions(Action<bool> callback);
        void RegisterPurchaseDeferredListener(Action<Product> callback);

        void SetApplicationUsername(string applicationUsername);

        bool simulateAskToBuy { get; set; }

        void SetStorePromotionOrder(List<Product> products);
        void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible);

        void ContinuePromotionalPurchases();
        Dictionary<string, string> GetIntroductoryPriceDictionary();
        Dictionary<string, string> GetProductDetails();

        /// <summary>
        /// Initiate Apple iOS 14 Subscription Offer Code redemption API, presentCodeRedemptionSheet
        /// </summary>
        void PresentCodeRedemptionSheet();
    }

    // Converted to a string (ToString) to pass to Apple native code, so do not change these names.
    public enum AppleStorePromotionVisibility
    {
        Default,
        Hide,
        Show
    }
}
