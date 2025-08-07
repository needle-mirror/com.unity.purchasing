#nullable enable

using System;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayGetGooglePurchaseUseCase : IGooglePlayGetGooglePurchaseUseCase
    {
        readonly IStore m_Store;

        [Preserve]
        public GooglePlayGetGooglePurchaseUseCase(IStore store)
        {
            m_Store = store;
        }

        public IGooglePurchase? GetGooglePurchase(string purchaseToken)
        {
            var googlePurchase = GooglePlayStore()?.GetGooglePurchase(purchaseToken);
            return googlePurchase;
        }

        IGooglePlayStore? GooglePlayStore()
        {
            return m_Store as IGooglePlayStore;
        }
    }
}
