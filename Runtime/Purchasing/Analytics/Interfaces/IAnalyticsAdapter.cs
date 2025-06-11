namespace UnityEngine.Purchasing
{
    interface IAnalyticsAdapter
    {
        void SendTransactionEvent(CartItem item, string receipt);
        void SendTransactionFailedEvent(PurchaseFailureDescription failureDescription);
    }
}
