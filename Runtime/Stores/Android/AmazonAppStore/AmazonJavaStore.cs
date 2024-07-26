#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class AmazonJavaStore : IAmazonJavaStore
    {
        private readonly AndroidJavaObject m_Store;

        public AndroidJavaObject GetStore()
        {
            return m_Store;
        }

        internal AmazonJavaStore(AndroidJavaObject store)
        {
            m_Store = store;
        }

        public void Connect()
        {
            m_Store.Call("Connect");
        }

        public void RetrieveProducts(string json)
        {
            m_Store.Call("RetrieveProducts", json);
        }

        public void FetchExistingPurchases()
        {
            m_Store.Call("FetchPurchases");
        }

        public virtual void Purchase(string productJSON, string developerPayload)
        {
            m_Store.Call("Purchase", productJSON, developerPayload);
        }

        public virtual void FinishTransaction(string productJSON, string transactionID)
        {
            m_Store.Call("FinishTransaction", productJSON, transactionID);
        }

        public bool CheckEntitlement(string productJSON)
        {
            throw new NotImplementedException();
        }

        public string GetAmazonUserId()
        {
            return m_Store.Call<string>("getAmazonUserId");
        }

        public void NotifyUnableToFulfillUnavailableProduct(string transactionID)
        {
            m_Store.Call("notifyUnableToFulfillUnavailableProduct", transactionID);
        }

        public void WriteSandboxJSON(HashSet<ProductDefinition> products)
        {
            m_Store.Call("writeSandboxJSON", JSONSerializer.SerializeProductDefs(products));
        }
    }
}
