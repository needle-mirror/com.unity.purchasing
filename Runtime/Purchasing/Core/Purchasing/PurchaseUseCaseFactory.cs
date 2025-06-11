using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    static class PurchaseUseCaseFactory
    {
        internal static IPurchaseUseCase Create(IStore store, IProductCache productCache)
        {
            switch (store)
            {
                case IGooglePlayStore googlePlayStore:
                    return new GooglePlayPurchaseUseCase(googlePlayStore, productCache);

                default:
                    return new PurchaseUseCase(store);
            }
        }
    }
}
