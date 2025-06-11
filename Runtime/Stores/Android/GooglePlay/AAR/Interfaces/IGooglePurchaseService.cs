#nullable enable

using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseService
    {
        void Purchase(ProductDefinition product, Order? currentOrder, GooglePlayReplacementMode? desiredReplacementMode);
        void SetProductCache(IProductCache productCache);
    }
}
