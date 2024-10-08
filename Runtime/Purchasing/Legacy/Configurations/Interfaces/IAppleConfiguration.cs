using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access Apple store specific configurations.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public interface IAppleConfiguration : IStoreConfiguration
    {
        /// <summary>
        /// Read the App Receipt from local storage.
        /// Returns null for iOS less than or equal to 6, may also be null on a reinstalling and require refreshing.
        /// </summary>
        string appReceipt { get; }

        /// <summary>
        /// Determine if the user can make payments; [SKPaymentQueue canMakePayments].
        /// </summary>
        bool canMakePayments { get; }

        /// <summary>
        /// Stores a callback that will be called when
        /// the user attempts a promotional purchase
        /// (directly from the Apple App Store) on
        /// iOS or tvOS.
        ///
        /// If the callback is set, you must call
        /// IAppleExtensions.ContinuePromotionalPurchases()
        /// inside it in order to continue the intercepted
        /// purchase(s).
        /// </summary>
        /// <param name="callback"></param>
        void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback);

        /// <summary>
        /// Called when entitlements are revoked from Apple.
        /// </summary>
        /// <param name="callback">Action will be called with the products that were revoked.</param>
        void SetEntitlementsRevokedListener(Action<List<Product>> callback);
    }
}
