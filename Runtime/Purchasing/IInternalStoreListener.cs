#nullable enable
using UnityEngine.Purchasing.Extension;
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Internal Purchasing interface.
    /// </summary>
    internal interface IInternalStoreListener
    {
        /// <summary>
        /// Purchasing failed to initialise for a non recoverable reason.
        /// </summary>
        void OnInitializeFailed(InitializationFailureReason error, string? message = null);

        /// <summary>
        /// A purchase succeeded.
        /// </summary>
        PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e);

        /// <summary>
        /// A purchase failed with specified reason.
        /// </summary>
        void OnPurchaseFailed(Product i, PurchaseFailureDescription p);

        /// <summary>
        /// Purchasing initialized successfully.
        ///
        /// The <c>IStoreController</c> is available for accessing
        /// purchasing functionality.
        ///
        /// Initialized products will include receipts, if currently owned.
        /// </summary>
        void OnInitialized(IStoreController controller);
    }
}
