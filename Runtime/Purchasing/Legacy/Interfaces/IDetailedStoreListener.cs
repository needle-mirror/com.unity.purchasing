using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public interface IDetailedStoreListener : IStoreListener
    {
        /// <summary>
        /// A purchase failed with a detailed Failure Description.
        /// PurchaseFailureDescription contains : productId, PurchaseFailureReason and an error message
        /// </summary>
        /// <param name="product"> The product that was attempted to be purchased. </param>
        /// <param name="failureDescription"> The Purchase Failure Description. </param>
        void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription);
    }
}
