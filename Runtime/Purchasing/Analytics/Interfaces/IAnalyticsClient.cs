using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    internal interface IAnalyticsClient
    {
        void OnPurchaseSucceeded(Product product);
        void OnPurchaseFailed(Product product, PurchaseFailureDescription purchaseFailureDescription);
    }
}
