using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreFinishTransactionService
    {
        void SetProductCache(IProductCache productCache);
        void SetConfirmCallback(IStorePurchaseConfirmCallback confirmCallback);
        void FinishTransaction(ProductDefinition product, string purchaseToken);
    }
}
