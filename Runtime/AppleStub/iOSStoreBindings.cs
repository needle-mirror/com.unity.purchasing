using System;

namespace UnityEngine.Purchasing
{
    internal class iOSStoreBindings : INativeAppleStore
    {
        public void SetUnityPurchasingCallback(UnityPurchasingCallback AsyncCallback)
        {
            throw new NotImplementedException();
        }
        public void RestoreTransactions()
        {
            throw new NotImplementedException();
        }

        public void SetAppAccountToken(string token)
        {
            throw new NotImplementedException();
        }

        public string AppReceipt()
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void AddTransactionObserver()
        {
            throw new NotImplementedException();
        }

        public void FetchProducts(string json)
        {
            throw new NotImplementedException();
        }
        public void FetchExistingPurchases()
        {
            throw new NotImplementedException();
        }
        public void Purchase(string productJSON, string developerPayload)
        {
            throw new NotImplementedException();
        }
        public void FinishTransaction(string productJSON, string transactionID)
        {
            throw new NotImplementedException();
        }

        public bool CheckEntitlement(string productJSON)
        {
            throw new NotImplementedException();
        }

        public bool canMakePayments => throw new NotImplementedException();

        public void FetchStorePromotionOrder()
        {
            throw new NotImplementedException();
        }

        public void SetStorePromotionOrder(string json)
        {
            throw new NotImplementedException();
        }

        public void FetchStorePromotionVisibility(string productId)
        {
            throw new NotImplementedException();
        }

        public void SetStorePromotionVisibility(string productId, string visibility)
        {
            throw new NotImplementedException();
        }

        public void InterceptPromotionalPurchases()
        {
            throw new NotImplementedException();
        }

        public void ContinuePromotionalPurchases()
        {
            throw new NotImplementedException();
        }

        public void PresentCodeRedemptionSheet()
        {
            throw new NotImplementedException();
        }

        public void DeallocateMemory(IntPtr pointer)
        {
            throw new NotImplementedException();
        }

        // TODO: IAP-3929
        public void RefreshAppReceipt()
        {
            throw new NotImplementedException();
        }

        public void FetchPurchases(string json)
        {
            throw new NotImplementedException();
        }

        public void Purchase(string productJson, string optionsJson, StorefrontChangeCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
