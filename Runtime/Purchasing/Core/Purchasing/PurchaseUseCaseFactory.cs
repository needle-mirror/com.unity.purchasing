using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    static class PurchaseUseCaseFactory
    {
        internal static IPurchaseUseCase Create(IStore store)
        {
            switch (store)
            {
                case IGooglePlayStore googlePlayStore:
                    return new GooglePlayPurchaseUseCase(googlePlayStore);

                default:
                    return new PurchaseUseCase(store);
            }
        }
    }
}
