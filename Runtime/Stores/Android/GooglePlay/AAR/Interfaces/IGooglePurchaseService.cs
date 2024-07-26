using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseService
    {
        void Purchase(ProductDefinition product, Product oldProduct, GooglePlayProrationMode? desiredProrationMode);
        void SetProductCache(IProductCache productCache);
    }
}
