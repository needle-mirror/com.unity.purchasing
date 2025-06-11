using System;

namespace UnityEngine.Purchasing
{
    interface INativeAppleStore : INativeStore
    {
        void SetUnityPurchasingCallback(UnityPurchasingCallback asyncCallback);
        void RestoreTransactions();
        void AddTransactionObserver();
        string AppReceipt();
        bool canMakePayments { get; }
        void FetchStorePromotionOrder();
        void SetStorePromotionOrder(string json);
        void FetchStorePromotionVisibility(string productId);
        void SetStorePromotionVisibility(string productId, string visibility);
        void InterceptPromotionalPurchases();
        void ContinuePromotionalPurchases();
        void PresentCodeRedemptionSheet();
        void DeallocateMemory(IntPtr pointer);
        void RefreshAppReceipt();
    }
}
