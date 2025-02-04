using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    interface IAppleStoreCallbacks
    {
        void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback);
        void SetFetchStorePromotionOrderCallbacks(Action<List<Product>> successCallback, Action<string> errorCallback);
        void SetFetchStorePromotionVisibilityCallbacks(Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback);
        void SetRestoreTransactionsCallback(Action<bool, string> successCallback);
        void ClearTransactionLog();
        bool simulateAskToBuy { get; set; }
        void SetAppAccountToken(Guid token);
    }
}
