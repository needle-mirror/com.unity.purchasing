using System;

namespace UnityEngine.Purchasing
{
    delegate void ExternalPurchaseCallback(IntPtr subjectPtr, IntPtr payloadPtr);
    delegate void SceneBecameFrontmostCallback();

    interface INativeAppleStore : INativeStore
    {
        void SetUnityPurchasingCallback(UnityPurchasingCallback asyncCallback);
        // StoreKit 2 purchase; the answer echoes impressionId back in its payload.
        void Purchase(string productJson, string optionsJson, string impressionId);
        void Sk1SetUnityPurchasingCallback(Sk1UnityPurchasingCallback asyncCallback);
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
        void SetApplicationUsername(string guid);
        void TransactionObserved(string transactionId, string productId, string productJsonRepresentation, double transactionUnixTime, string transactionJsonRepresentation, string signatureJws);
        void FetchStorefront();
        void ExternalPurchaseCheckEligibility(ExternalPurchaseCallback callback);
        void ExternalPurchaseFetchToken(string tokenType, ExternalPurchaseCallback callback);
        void ExternalPurchaseShowNotice(string noticeType, ExternalPurchaseCallback callback);
        void ExternalPurchaseFetchStorefront(ExternalPurchaseCallback callback);
        string FetchAdvertisingIdentifier();
        string FetchVendorIdentifier();
        string FetchNativeDeviceInfo();
        // Only false on iPad, while another app's window covers the app.
        bool IsSceneFrontmost();
        // iPad only: called when the app comes back in front.
        void SetSceneBecameFrontmostCallback(SceneBecameFrontmostCallback callback);
    }
}
