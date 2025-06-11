using System;
using System.Runtime.InteropServices;

#if !UNITY_EDITOR
namespace UnityEngine.Purchasing
{
    internal class iOSStoreBindings : INativeAppleStore
    {
        [DllImport("__Internal")]
        static extern void unityPurchasing_SetNativeCallback(UnityPurchasingCallback callbackDelegate);

        [DllImport("__Internal")]
        static extern void unityPurchasing_AddTransactionObserver();

        [DllImport("__Internal")]
        static extern void unityPurchasing_FetchProducts(string json);

        [DllImport("__Internal")]
        static extern void unityPurchasing_PurchaseProduct(string productJson, string optionsJson, StorefrontChangeCallback storefrontCallbackDelegate);

        [DllImport("__Internal")]
        static extern IntPtr unityPurchasing_FetchAppReceipt();

        [DllImport("__Internal")]
        static extern void unityPurchasing_DeallocateMemory(IntPtr pointer);

        [DllImport("__Internal")]
        static extern void unityPurchasing_FetchPurchases();

        // TODO: IAP-3857
        [DllImport("__Internal")]
        static extern string unityPurchasing_FetchTransactionForProductId(string productId);

        [DllImport("__Internal")]
        static extern void unityPurchasing_FinishTransaction(string transactionId, bool logFinishTransaction);

        [DllImport("__Internal")]
        static extern bool unityPurchasing_checkEntitlement(string productId);

        [DllImport("__Internal")]
        private static extern void unityPurchasing_RestoreTransactions();

        [DllImport("__Internal")]
        private static extern bool unityPurchasing_CanMakePayments();

        [DllImport("__Internal")]
        private static extern void unityPurchasing_FetchStorePromotionOrder();

        [DllImport("__Internal")]
        private static extern void unityPurchasing_UpdateStorePromotionOrder(string json);

        [DllImport("__Internal")]
        private static extern void unityPurchasing_FetchStorePromotionVisibility(string productId);

        [DllImport("__Internal")]
        private static extern void unityPurchasing_UpdateStorePromotionVisibility(string productId, string visibility);

        [DllImport("__Internal")]
        private static extern void unityPurchasing_InterceptPromotionalPurchases();

        [DllImport("__Internal")]
        private static extern void unityPurchasing_ContinuePromotionalPurchases();

        [DllImport("__Internal")]
        private static extern void unityPurchasing_PresentCodeRedemptionSheet();

        // TODO: IAP-3929
        [DllImport("__Internal")]
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
            // Moved AddTransactionObserver to after we fetched the products to avoid Unknown ProductType errors
            // No need to connect on Apple as it is automatically done
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

        public bool CheckEntitlement(string productId)
        {
            return unityPurchasing_checkEntitlement(productId);
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
