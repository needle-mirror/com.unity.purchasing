#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Provides fake functionality for Apple specific APIs.
    ///
    /// Refresh receipt calls alternate between success and failure.
    /// </summary>
    class FakeAppleExtensions : IAppleExtensions
    {
        bool m_FailRefresh;

        public void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
            if (m_FailRefresh)
            {
                errorCallback("A fake error message");
            }
            else
            {
                successCallback("A fake refreshed receipt!");
            }

            m_FailRefresh = !m_FailRefresh;
        }

        [Obsolete("RefreshAppReceipt(Action<string> successCallback, Action errorCallback) is deprecated, please use RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback) instead.")]
        public void RefreshAppReceipt(Action<string> successCallback, Action errorCallback)
        {
            if (m_FailRefresh)
            {
                errorCallback();
            }
            else
            {
                successCallback("A fake refreshed receipt!");
            }

            m_FailRefresh = !m_FailRefresh;
        }

        [Obsolete("RestoreTransactions(Action<bool> callback) is deprecated, please use RestoreTransactions(Action<bool, string> callback) instead.")]
        public void RestoreTransactions(Action<bool>? callback)
        {
            callback?.Invoke(true);
        }

        public void RestoreTransactions(Action<bool, string?>? callback)
        {
            callback?.Invoke(true, null);
        }

        public void RegisterPurchaseDeferredListener(Action<Product> callback)
        {
        }

        public bool simulateAskToBuy
        {
            get;
            set;
        }

        public void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            errorCallback();
        }

        public void SetStorePromotionOrder(List<Product> products)
        {
        }

        public void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            errorCallback();
        }

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)
        {
        }

        public void SetApplicationUsername(string applicationUsername)
        {
        }

        public string GetTransactionReceiptForProduct(Product product)
        {
            return "";
        }

        public void ContinuePromotionalPurchases()
        {
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {
            return new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetProductDetails()
        {
            return new Dictionary<string, string>();
        }

        public void PresentCodeRedemptionSheet()
        {
        }
    }
}
