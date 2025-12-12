using System;
using System.Runtime.InteropServices;
using UnityEngine.Purchasing.Extension;
using Purchasing.Utilities;

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
        static extern bool unityPurchasing_checkEntitlement(string productId);

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


#region StoreKit1Bindings
        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingRetrieveProducts(string json);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingPurchase(string json, string developerPayload);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingFinishTransaction(string productJSON, string transactionId);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingRestoreTransactions();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingRefreshAppReceipt();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingAddTransactionObserver();

        [DllImport ("unitypurchasing")]
        private static extern void unityPurchasingSetApplicationUsername(string username);

        [DllImport("unitypurchasing")]
        private static extern void setUnityPurchasingCallback (Sk1UnityPurchasingCallback AsyncCallback);

        [DllImport("unitypurchasing")]
        private static extern string getUnityPurchasingAppReceipt ();

        [DllImport("unitypurchasing")]
        private static extern string getUnityPurchasingTransactionReceiptForProductId (string productId);

        [DllImport("unitypurchasing")]
        private static extern bool getUnityPurchasingCanMakePayments ();

        [DllImport ("unitypurchasing")]
        private static extern void setSimulateAskToBuy (bool enabled);

        [DllImport ("unitypurchasing")]
        private static extern bool getSimulateAskToBuy ();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingFetchStorePromotionOrder();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingUpdateStorePromotionOrder(string json);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingFetchStorePromotionVisibility(string productId);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingUpdateStorePromotionVisibility(string productId, string visibility);

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingInterceptPromotionalPurchases ();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingContinuePromotionalPurchases ();

        [DllImport("unitypurchasing")]
        private static extern void unityPurchasingPresentCodeRedemptionSheet ();

        public bool useStoreKit1 = StoreKitSelector.UseStoreKit1();

        public void Sk1SetUnityPurchasingCallback(Sk1UnityPurchasingCallback AsyncCallback)
        {
            setUnityPurchasingCallback(AsyncCallback);
        }
#endregion

        public void SetUnityPurchasingCallback(UnityPurchasingCallback AsyncCallback)
        {
            unityPurchasing_SetNativeCallback(AsyncCallback);
        }

        public string AppReceipt()
        {
            if (useStoreKit1)
            {
                // For StoreKit1, we use getUnityPurchasingAppReceipt
                return getUnityPurchasingAppReceipt();
            }

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
            if (useStoreKit1)
            {
                return;
            }

            unityPurchasing_DeallocateMemory(pointer);
        }

        public bool canMakePayments
        {
            get
            {
                if (useStoreKit1)
                    {
                        return getUnityPurchasingCanMakePayments();
                    }

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
            if (useStoreKit1)
            {
                unityPurchasingAddTransactionObserver();
                return;
            }

            unityPurchasing_AddTransactionObserver();
        }

        public void SetApplicationUsername (string applicationUsername)
        {
            if (useStoreKit1)
                unityPurchasingSetApplicationUsername(applicationUsername);
        }

        public void FetchProducts(string json)
        {
            if (useStoreKit1)
            {
                unityPurchasingRetrieveProducts(json);
                return;
            }

            unityPurchasing_FetchProducts(json);
        }

        public void FetchExistingPurchases()
        {
            // This shouldn't be reachable via StoreKit 1, but just in case
            if (useStoreKit1)
            {
                return;
            }
            unityPurchasing_FetchPurchases();
        }

        public void Purchase(string productJSON, string developerPayload)
        {
            if (useStoreKit1)
            {
                unityPurchasingPurchase(productJSON, developerPayload);
                return;
            }

            Purchase(productJSON, developerPayload, null);
        }

        public void FinishTransaction(string productDescription, string transactionId)
        {
            if (useStoreKit1)
            {
                unityPurchasingFinishTransaction(productDescription, transactionId);
                return;
            }

            unityPurchasing_FinishTransaction(transactionId, !string.IsNullOrEmpty(productDescription));
        }

        public bool CheckEntitlement(string productId)
        {
            // This shouldn't be reachable via StoreKit 1, but just in case
            if (useStoreKit1)
            {
                return false;
            }
            return unityPurchasing_checkEntitlement(productId);
        }

        public void RestoreTransactions()
        {
            if (useStoreKit1)
            {
                unityPurchasingRestoreTransactions();
                return;
            }

            unityPurchasing_RestoreTransactions();
        }

        public void FetchStorePromotionOrder()
        {
            if (useStoreKit1)
            {
                unityPurchasingFetchStorePromotionOrder();
                return;
            }

            unityPurchasing_FetchStorePromotionOrder();
        }

        public void SetStorePromotionOrder(string json)
        {
            if (useStoreKit1)
            {
                unityPurchasingUpdateStorePromotionOrder(json);
                return;
            }

            unityPurchasing_UpdateStorePromotionOrder(json);
        }

        public void FetchStorePromotionVisibility(string productId)
        {
            if (useStoreKit1)
            {
                unityPurchasingFetchStorePromotionVisibility(productId);
                return;
            }

            unityPurchasing_FetchStorePromotionVisibility(productId);
        }

        public void SetStorePromotionVisibility(string productId, string visibility)
        {
            if (useStoreKit1)
            {
                unityPurchasingUpdateStorePromotionVisibility(productId, visibility);
                return;
            }

            unityPurchasing_UpdateStorePromotionVisibility(productId, visibility);
        }

        public void InterceptPromotionalPurchases()
        {
            if (useStoreKit1)
            {
                unityPurchasingInterceptPromotionalPurchases();
                return;
            }

            unityPurchasing_InterceptPromotionalPurchases();
        }

        public void ContinuePromotionalPurchases()
        {
            if (useStoreKit1)
            {
                unityPurchasingContinuePromotionalPurchases();
                return;
            }

            unityPurchasing_ContinuePromotionalPurchases();
        }

        public void PresentCodeRedemptionSheet()
        {
            if (useStoreKit1)
            {
                unityPurchasingPresentCodeRedemptionSheet();
                return;
            }

            unityPurchasing_PresentCodeRedemptionSheet();
        }

        public void Purchase(string productJson, string optionsJson, StorefrontChangeCallback callback)
        {
            if (useStoreKit1)
            {
                unityPurchasingPurchase(productJson, optionsJson);
                return;
            }

            unityPurchasing_PurchaseProduct(productJson, optionsJson, callback);
        }

        // TODO: IAP-3929
        public void RefreshAppReceipt()
        {
            if (useStoreKit1)
            {
                unityPurchasingRefreshAppReceipt();
                return;
            }

            unityPurchasing_RefreshAppReceipt();
        }
    }
}
#endif
