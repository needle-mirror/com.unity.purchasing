#nullable enable

using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePurchaseConverter
    {
        Order CreateOrderFromPurchase(IGooglePurchase purchase, IProductCache? productCache);
        ICart CreateCartFromPurchase(IGooglePurchase purchase, IProductCache? productCache);
    }
}
