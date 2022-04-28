namespace UnityEngine.Purchasing
{
    interface IAnalyticsAdapter
    {
        void SendTransactionEvent(Product product);
        void SendTransactionFailedEvent(Product product, PurchaseFailureReason reason);
    }
}
