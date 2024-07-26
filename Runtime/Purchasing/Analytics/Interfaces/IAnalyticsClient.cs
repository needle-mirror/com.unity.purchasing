using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IAnalyticsClient
    {
        void OnPurchaseSucceeded(ConfirmedOrder confirmedOrder);
        void OnPurchaseFailed(FailedOrder failedOrder);
    }
}
