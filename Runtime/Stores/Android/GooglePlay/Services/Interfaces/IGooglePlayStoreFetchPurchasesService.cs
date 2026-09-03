using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreFetchPurchasesService
    {
        void SetProductCache(IProductCache productCache);
        void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchCallback);
        void FetchPurchases();
        IGooglePurchase GetGooglePurchase(string purchaseToken);
    }
}
