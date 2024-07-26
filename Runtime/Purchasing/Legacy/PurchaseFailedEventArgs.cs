using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A purchase that failed including the product under purchase,
    /// the reason for the failure and any additional information.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public class PurchaseFailedEventArgs
    {
        internal PurchaseFailedEventArgs(Product purchasedProduct, PurchaseFailureReason reason, string message)
        {
            this.purchasedProduct = purchasedProduct;
            this.reason = reason;
            this.message = message;
        }

        /// <summary>
        /// The product which failed to be purchased.
        /// </summary>
        public Product purchasedProduct { get; private set; }

        /// <summary>
        /// The reason for the failure.
        /// </summary>
        public PurchaseFailureReason reason { get; private set; }

        /// <summary>
        /// A message containing details about the failure.
        /// </summary>
        public string message { get; private set; }
    }
}
