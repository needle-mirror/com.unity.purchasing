using System;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
    // internal delegate void UnityPurchasingCallback(string subject, string payload, string receipt, string transactionId);

    internal interface INativeFacebookStore : INativeStore
    {
        bool Check();
        void Init();
        void SetUnityPurchasingCallback (UnityPurchasingCallback AsyncCallback);

        bool ConsumeItem (string purchaseToken);
        // any others we need?
    }
}
