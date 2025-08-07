#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Apple Store purchase service extension.
    /// </summary>
    public interface IAppleStoreExtendedPurchaseService : IPurchaseServiceExtension
    {
        /// <summary>
        /// For testing purposes only.
        ///
        /// Modify payment request for testing ask-to-buy.
        /// </summary>
        bool simulateAskToBuy { get; set; }

        /// <summary>
        /// Initiate Apple Subscription Offer Code redemption API, presentCodeRedemptionSheet
        /// </summary>
        void PresentCodeRedemptionSheet();

        /// <summary>
        /// Continue promotional purchases that were intercepted by `OnPromotionalPurchaseIntercepted`
        /// </summary>
        void ContinuePromotionalPurchases();

        /// <summary>
        /// Read the App Receipt from local storage.
        /// Returns null for iOS less than or equal to 6, may also be null on a reinstalling and require refreshing.
        /// </summary>
        [Obsolete]
        string? appReceipt { get; }

        /// <summary>
        /// Callback when an entitlement is revoked.
        /// The callback will return the product id that has been revoked.
        /// </summary>
        public event Action<string>? OnEntitlementRevoked;

        /// <summary>
        /// Callback that will be called when the user attempts a promotional purchase
        /// (directly from the Apple App Store) on iOS or tvOS.
        /// The callback must be set before fetching products.
        ///
        /// If the callback is set, you must call `ContinuePromotionalPurchases()`
        /// inside it in order to continue the intercepted purchase(s).
        /// </summary>
        event Action<Product>? OnPromotionalPurchaseIntercepted;

        // TODO: IAP-3929
        /// <summary>
        /// Fetch the latest App Receipt from Apple.
        /// This requires an Internet connection and will prompt the user for their credentials.
        /// </summary>
        /// <param name="successCallback">This action will be called when the refresh is successful. The receipt will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the refresh is in error. The error's details will be passed through.</param>
        [Obsolete]
        void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback);

        // TODO: IAP-3929
        /// <summary>
        /// Indicates whether to automatically refresh the app receipt when a purchase is made.
        /// This is useful if you are still using the receipt to validate purchases.
        /// True by default.
        /// </summary>
        /// <param name="refreshAppReceipt">Whether to refresh the app receipt automatically after a purchase.</param>
        [Obsolete]
        void SetRefreshAppReceipt(bool refreshAppReceipt);
    }
}
