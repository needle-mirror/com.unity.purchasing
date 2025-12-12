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

        internal PendingOrder(ICart cart, IOrderInfo info, IAppleTransactionSubscriptionInfo? subscriptionInfo) : base(cart, info)
        {
            info.PurchasedProductInfo = FillPurchasedProductInfo(subscriptionInfo);
        }

        List<IPurchasedProductInfo> FillPurchasedProductInfo(IAppleTransactionSubscriptionInfo? appleTransactionSubscriptionInfo = null)
        {
            // TODO: ULO-8118 Move IOrderInfo population to outside of orders.
            var purchasedProductInfo = new List<IPurchasedProductInfo>();

            var cartItems = CartOrdered.Items();
            if (cartItems == null)
            {
                return purchasedProductInfo;
            }

            foreach (var cartItem in cartItems)
            {
                var productDefinition = cartItem?.Product.definition;

                var productId = productDefinition?.storeSpecificId;
                var productType = productDefinition?.type;

                if (productId != null)
                {
                    purchasedProductInfo.Add(new PurchasedProductInfo(productId, Info.Receipt, productType ?? ProductType.Unknown, appleTransactionSubscriptionInfo));
                }
                else
                {
                    purchasedProductInfo.Add(new UnknownPurchasedProductInfo());
                }
            }

            return purchasedProductInfo;
        }
    }
}
