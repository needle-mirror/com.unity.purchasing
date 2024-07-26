#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Receives callbacks from Android based stores.
    /// </summary>
    internal class JavaBridge : AndroidJavaProxy, IUnityCallback
    {
        private readonly IUnityCallback forwardTo;

        public JavaBridge(IUnityCallback forwardTo)
            : base("com.unity.purchasing.common.IUnityCallback")
        {
            this.forwardTo = forwardTo;
        }

        public JavaBridge(IUnityCallback forwardTo, string javaInterface)
            : base(javaInterface)
        {
            this.forwardTo = forwardTo;
        }

        public void OnProductsRetrieved(String json)
        {
            forwardTo.OnProductsRetrieved(json);
        }

        public void OnProductsRetrieveFailed(string jsonFailureDescription)
        {
            forwardTo.OnProductsRetrieveFailed(jsonFailureDescription);
        }

        public void OnPurchasesRetrievalFailed(string jsonFailureDescription)
        {
            forwardTo.OnPurchasesRetrievalFailed(jsonFailureDescription);
        }

        public void OnPurchasesFetched(string json)
        {
            forwardTo.OnPurchasesFetched(json);
        }

        public void OnPurchaseSucceeded(String id, String receipt, String transactionID)
        {
            forwardTo.OnPurchaseSucceeded(id, receipt, transactionID);
        }

        public void OnPurchaseFailed(String json)
        {
            forwardTo.OnPurchaseFailed(json);
        }

        public void OnStoreConnectionSucceeded()
        {
            forwardTo.OnStoreConnectionSucceeded();
        }

        public void OnStoreConnectionFailed(string jsonFailureDescription)
        {
            forwardTo.OnStoreConnectionFailed(jsonFailureDescription);
        }
    }
}
