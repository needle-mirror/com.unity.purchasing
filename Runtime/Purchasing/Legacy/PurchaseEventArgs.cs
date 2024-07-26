using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A purchase that succeeded, including the purchased product
    /// along with its purchase receipt.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public class PurchaseEventArgs
    {
        internal PurchaseEventArgs(Product purchasedProduct)
        {
            this.purchasedProduct = purchasedProduct;
        }

        /// <summary>
        /// The product which was purchased successfully.
        /// </summary>
        public Product purchasedProduct { get; private set; }
    }
}
