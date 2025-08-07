using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreFetchPurchasesService
    {
        void SetProductCache(IProductCache productCache);
        void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchCallback);
        void FetchPurchases();
        void FetchPurchases(Action<List<Product>> onQueryPurchaseSucceed);
        IGooglePurchase GetGooglePurchase(string purchaseToken);
    }
}
