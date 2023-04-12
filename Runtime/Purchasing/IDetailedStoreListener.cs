using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
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
