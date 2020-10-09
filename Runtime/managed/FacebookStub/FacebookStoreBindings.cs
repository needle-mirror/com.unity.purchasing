using System;

namespace UnityEngine.Purchasing
{
    internal class FacebookStoreBindings : INativeFacebookStore
    {
        public bool Check()
        {
            return false;
        }

        public void Init()
        {
            throw new NotImplementedException ();
        }
                public void SetUnityPurchasingCallback (UnityPurchasingCallback AsyncCallback)
        {
            throw new NotImplementedException ();
        }
        public void RetrieveProducts (string json)
        {
            throw new NotImplementedException ();
        }
        public void Purchase (string productJSON, string developerPayload)
        {
            throw new NotImplementedException ();
        }
        public void FinishTransaction (string productJSON, string transactionID)
        {
            throw new NotImplementedException ();
        }
        public bool ConsumeItem (string purchaseToken)
        {
            throw new NotImplementedException ();
        }


    }
}

