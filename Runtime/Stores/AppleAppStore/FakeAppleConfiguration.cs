#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class FakeAppleConfiguration : IAppleConfiguration
    {
        public string appReceipt => "This is a fake receipt. When running on an Apple store, a base64 encoded App Receipt would be returned";

        public bool canMakePayments => true;

        public void SetApplePromotionalPurchaseInterceptorCallback(Action<Product> callback)
        {
        }

        public void SetEntitlementsRevokedListener(Action<List<Product>> callback)
        {
        }
    }
}
