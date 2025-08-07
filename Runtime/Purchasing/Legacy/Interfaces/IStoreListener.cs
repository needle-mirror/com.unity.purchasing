using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Implemented by Application developers using Unity Purchasing.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public interface IStoreListener
    {
        /// <summary>
        /// Purchasing initialized successfully.
        ///
        /// The <c>IStoreController</c> and <c>IExtensionProvider</c> are
        /// available for accessing purchasing functionality.
        /// </summary>
        /// <param name="controller"> The <c>IStoreController</c> created during initialization. </param>
        /// <param name="extensions"> The <c>IExtensionProvider</c> created during initialization. </param>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions);
        /// <summary>
        /// Purchasing failed to initialise for a non recoverable reason.
        /// </summary>
        /// <param name="error"> The failure reason. </param>
        [Obsolete]
        public void OnInitializeFailed(InitializationFailureReason error);
        /// <summary>
        /// Purchasing failed to initialise for a non recoverable reason.
        /// </summary>
        /// <param name="error"> The failure reason. </param>
        /// <param name="message"> More detail on the error : for example the GoogleBillingResponseCode. </param>
        public void OnInitializeFailed(InitializationFailureReason error, string message = null);
        /// <summary>
        /// A purchase succeeded.
        /// </summary>
        /// <param name="args"> The <c>PurchaseEventArgs</c> for the purchase event. </param>
        /// <returns> The result of the successful purchase </returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args);
        /// <summary>
        /// A purchase failed with specified reason.
        /// </summary>
        /// <param name="product"> The product that was attempted to be purchased. </param>
        /// <param name="failureReason"> The failure reason. </param>
        [Obsolete("Use IDetailedStoreListener.OnPurchaseFailed for more detailed callback.", false)]
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason);
    }
}
