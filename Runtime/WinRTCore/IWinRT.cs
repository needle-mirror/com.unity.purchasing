
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Default {

    public interface IWindowsIAPCallback {
        void OnProductListReceived(WinProductDescription[] winProducts);
        void OnProductListError(string message);
        void OnPurchaseSucceeded(string productId, string receipt, string transactionId);
        void OnPurchaseFailed(string productId, string error);
        void logError(string error);

        void log(string message);
    }

    public interface IWindowsIAP
    {
        void BuildDummyProducts(List<WinProductDescription> products);
        void Initialize(IWindowsIAPCallback callback);
        void RetrieveProducts(bool retryIfOffline);
        void Purchase(string productId);
        void FinaliseTransaction(string transactionId);
    }
}
