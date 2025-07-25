using System;
using System.Runtime.InteropServices;

#if !UNITY_EDITOR
namespace UnityEngine.Purchasing
{
    internal class OSXStoreBindings : INativeAppleStore
    {
        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_SetNativeCallback(UnityPurchasingCallback callbackDelegate);

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_AddTransactionObserver();

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_FetchProducts(string json);

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_PurchaseProduct(string productJson, string optionsJson, StorefrontChangeCallback storefrontCallbackDelegate);

        [DllImport("unitypurchasing")]
        static extern IntPtr unityPurchasing_FetchAppReceipt();

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_DeallocateMemory(IntPtr pointer);

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_FetchPurchases();

        // TODO: IAP-3857
        [DllImport("unitypurchasing")]
        static extern string unityPurchasing_FetchTransactionForProductId(string productId);

        [DllImport("unitypurchasing")]
        static extern void unityPurchasing_FinishTransaction(string transactionId, bool logFinishTransaction);

        [DllImport("unitypurchasing")]
        static extern bool unityPurchasing_checkEntitlement(string json);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_RestoreTransactions();

        [DllImport("unitypurchasing")]
        private static extern bool unityPurchasing_CanMakePayments();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_FetchStorePromotionOrder();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_UpdateStorePromotionOrder(string json);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_FetchStorePromotionVisibility(string productId);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_UpdateStorePromotionVisibility(string productId, string visibility);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_InterceptPromotionalPurchases();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_ContinuePromotionalPurchases();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_PresentCodeRedemptionSheet();

        // TODO: IAP-3929
        [DllImport("unitypurchasing")]
        private static extern void unityPurchasing_RefreshAppReceipt();

        public void SetUnityPurchasingCallback(UnityPurchasingCallback AsyncCallback)
        {
            unityPurchasing_SetNativeCallback(AsyncCallback);
        }

        public string AppReceipt()
        {
            // Fetch the receipt pointer from Swift
            IntPtr receiptPointer = unityPurchasing_FetchAppReceipt();
            string res = "";
            if (receiptPointer != IntPtr.Zero)
            {
                res = Marshal.PtrToStringAuto(receiptPointer);

                // Deallocate the memory when done
                DeallocateMemory(receiptPointer);
            }

            return res;
        }

        public void DeallocateMemory(IntPtr pointer)
        {
            unityPurchasing_DeallocateMemory(pointer);
        }

        public bool canMakePayments
        {
            get
            {
                return unityPurchasing_CanMakePayments();
            }
        }

        public void Connect()
        {
        }

        public void AddTransactionObserver()
        {
            unityPurchasing_AddTransactionObserver();
        }

        public void FetchProducts(string json)
        {
            unityPurchasing_FetchProducts(json);
        }

        public void FetchExistingPurchases()
        {
            unityPurchasing_FetchPurchases();
        }

        public void Purchase(string productJSON, string developerPayload)
        {
            Purchase(productJSON, developerPayload, null);
        }

        public void FinishTransaction(string productDescription, string transactionId)
        {
            unityPurchasing_FinishTransaction(transactionId, !string.IsNullOrEmpty(productDescription));
        }

        public bool CheckEntitlement(string productJSON)
        {
            return unityPurchasing_checkEntitlement(productJSON);
        }

        public void RestoreTransactions()
        {
            unityPurchasing_RestoreTransactions();
        }

        public void FetchStorePromotionOrder()
        {
            unityPurchasing_FetchStorePromotionOrder();
        }

        public void SetStorePromotionOrder(string json)
        {
            unityPurchasing_UpdateStorePromotionOrder(json);
        }

        public void FetchStorePromotionVisibility(string productId)
        {
            unityPurchasing_FetchStorePromotionVisibility(productId);
        }

        public void SetStorePromotionVisibility(string productId, string visibility)
        {
            unityPurchasing_UpdateStorePromotionVisibility(productId, visibility);
        }

        public void InterceptPromotionalPurchases()
        {
            unityPurchasing_InterceptPromotionalPurchases();
        }

        public void ContinuePromotionalPurchases()
        {
            unityPurchasing_ContinuePromotionalPurchases();
        }

        public void PresentCodeRedemptionSheet()
        {
            unityPurchasing_PresentCodeRedemptionSheet();
        }

        public void Purchase(string productJson, string optionsJson, StorefrontChangeCallback callback)
        {
            unityPurchasing_PurchaseProduct(productJson, optionsJson, callback);
        }

        // TODO: IAP-3929
        public void RefreshAppReceipt()
        {
            unityPurchasing_RefreshAppReceipt();
        }
    }
}
#endif
