using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreFinishTransactionService
    {
        void SetStoreCallback(IStoreCallback storeCallback);
        void FinishTransaction(ProductDefinition product, string purchaseToken);
    }
}
