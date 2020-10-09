using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IGoogleFetchPurchases
    {
        void SetStoreCallback(IStoreCallback storeCallback);
        void FetchPurchases();
    }
}
