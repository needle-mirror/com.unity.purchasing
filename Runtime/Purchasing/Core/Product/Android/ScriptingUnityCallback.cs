using System;
using Uniject;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Wraps an IUnityCallback executing methods on
    /// the scripting thread.
    /// </summary>
    internal class ScriptingUnityCallback : IUnityCallback
    {
        private readonly IUnityCallback forwardTo;
        private readonly IUtil util;

        public ScriptingUnityCallback(IUnityCallback forwardTo, IUtil util)
        {
            this.forwardTo = forwardTo;
            this.util = util;
        }

        public void OnProductsFetched(string json)
        {
            util.RunOnMainThread(() => forwardTo.OnProductsFetched(json));
        }

        public void OnProductsFetchFailed(string jsonFailureDescription)
        {
            util.RunOnMainThread(() => forwardTo.OnProductsFetchFailed(jsonFailureDescription));
        }

        public void OnPurchasesRetrievalFailed(string jsonFailureDescription)
        {
            util.RunOnMainThread(() => forwardTo.OnPurchasesRetrievalFailed(jsonFailureDescription));
        }

        public void OnPurchasesFetched(string json)
        {
            util.RunOnMainThread(() => forwardTo.OnPurchasesFetched(json));
        }

        public void OnPurchaseSucceeded(string id, string receipt, string transactionID)
        {
            util.RunOnMainThread(() => forwardTo.OnPurchaseSucceeded(id, receipt, transactionID));
        }

        public void OnPurchaseFailed(string json)
        {
            util.RunOnMainThread(() => forwardTo.OnPurchaseFailed(json));
        }

        public void OnPurchaseDeferred(string json)
        {
            util.RunOnMainThread(() => forwardTo.OnPurchaseDeferred(json));
        }

        public void OnStoreConnectionSucceeded()
        {
            util.RunOnMainThread(() => forwardTo.OnStoreConnectionSucceeded());
        }

        public void OnStoreConnectionFailed(string jsonFailureDescription)
        {
            util.RunOnMainThread(() => forwardTo.OnStoreConnectionFailed(jsonFailureDescription));
        }
    }
}
