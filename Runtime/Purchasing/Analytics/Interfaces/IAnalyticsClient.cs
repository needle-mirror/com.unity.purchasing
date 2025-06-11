namespace UnityEngine.Purchasing
{
    interface IAnalyticsClient
    {
        void OnPurchaseSucceeded(ConfirmedOrder confirmedOrder);
        void OnPurchaseFailed(FailedOrder failedOrder);
    }
}
