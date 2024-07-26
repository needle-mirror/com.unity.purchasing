using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// JSON based Native callback interface.
    /// </summary>
    internal interface IUnityCallback
    {
        void OnStoreConnectionSucceeded();
        void OnStoreConnectionFailed(string jsonFailureDescription);
        void OnProductsRetrieved(string json);
        void OnProductsRetrieveFailed(string jsonFailureDescription);
        void OnPurchasesRetrievalFailed(string jsonFailureDescription);
        void OnPurchasesFetched(string json);
        void OnPurchaseSucceeded(string id, string receipt, string transactionID);
        void OnPurchaseFailed(string json);
    }
}
