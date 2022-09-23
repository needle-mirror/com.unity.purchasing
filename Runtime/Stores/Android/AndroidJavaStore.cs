using System;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    internal class AndroidJavaStore : INativeStore
    {
        private readonly AndroidJavaObject m_Store;
        protected AndroidJavaObject GetStore()
        {
            return m_Store;
        }

        public AndroidJavaStore(AndroidJavaObject store)
        {
            m_Store = store;
        }

        public void RetrieveProducts(string json)
        {
            m_Store.Call("RetrieveProducts", json);
        }

        public virtual void Purchase(string productJSON, string developerPayload)
        {
            m_Store.Call("Purchase", productJSON, developerPayload);
        }

        public virtual void FinishTransaction(string productJSON, string transactionID)
        {
            m_Store.Call("FinishTransaction", productJSON, transactionID);
        }
    }
}
