#nullable enable

using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePurchaseConverter
    {
        Task<Order> CreateOrderFromPurchase(IGooglePurchase purchase, IProductCache? productCache);
        Task<ICart> CreateCartFromPurchase(IGooglePurchase purchase, IProductCache? productCache);
    }
}
