using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A model representing a pending order of a product.
    /// </summary>
    public class PendingOrder : Order
    {
        /// <summary>
        /// Constructs the pending order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        public PendingOrder(ICart cart, IOrderInfo info)
            : base(cart, info)
        {
            info.PurchasedProductInfo = FillPurchasedProductInfo();
        }

        List<IPurchasedProductInfo> FillPurchasedProductInfo()
        {
            var purchasedProductInfo = new List<IPurchasedProductInfo>();
            var productDefinition = CartOrdered.Items()?.FirstOrDefault()?.Product.definition;
            var productId = productDefinition?.storeSpecificId;
            var productType = productDefinition?.type;
            if (productId != null)
            {
                purchasedProductInfo.Add(new PurchasedProductInfo(productId, Info.Receipt, productType ?? ProductType.Unknown));
            }

            return purchasedProductInfo;
        }
    }
}
