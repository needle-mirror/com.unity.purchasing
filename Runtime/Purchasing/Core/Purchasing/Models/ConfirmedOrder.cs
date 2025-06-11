#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model representing a confirmed order of a product.
    /// </summary>
    public class ConfirmedOrder : Order
    {
        /// <summary>
        /// Constructs the confirmed order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        public ConfirmedOrder(ICart cart, IOrderInfo info)
            : base(cart, info)
        {
            info.PurchasedProductInfo = FillPurchasedProductInfo();
        }

        List<IPurchasedProductInfo> FillPurchasedProductInfo()
        {
            var purchasedProductInfo = new List<IPurchasedProductInfo>();
            var productId = CartOrdered.Items().First().Product.definition.storeSpecificId;
            var productType = CartOrdered.Items().First().Product.definition.type;
            if (productId != null)
            {
                purchasedProductInfo.Add(new PurchasedProductInfo(productId, Info.Receipt, productType));
            }

            return purchasedProductInfo;
        }
    }
}
