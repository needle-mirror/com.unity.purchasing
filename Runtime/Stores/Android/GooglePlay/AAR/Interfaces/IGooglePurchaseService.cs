using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseService
    {
        void Purchase(ProductDefinition product, Product oldProduct, GooglePlayReplacementMode? desiredReplacementMode);
        void SetProductCache(IProductCache productCache);
    }
}
