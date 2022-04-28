namespace UnityEngine.Purchasing
{
    internal interface IAnalyticsClient
    {
        void OnPurchaseSucceeded(Product product);
        void OnPurchaseFailed(Product product, PurchaseFailureReason reason);
    }
}
