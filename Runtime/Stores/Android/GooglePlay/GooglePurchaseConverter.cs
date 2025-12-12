#nullable enable

using System;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePurchaseConverter : IGooglePurchaseConverter
    {
        [Preserve]
        internal GooglePurchaseConverter(IProductDetailsConverter productDetailsConverter)
        { }

        public Order CreateOrderFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            var cart = CreateCartFromPurchase(purchase, productCache);
            var orderInfo = new GoogleOrderInfo(purchase.receipt, purchase.purchaseToken, GooglePlay.Name, purchase.obfuscatedAccountId, purchase.obfuscatedProfileId);

            if (purchase.IsPending())
            {
                return new DeferredOrder(cart, orderInfo);
            }

            // A consumable that was acknowledged should still be a PendingOrder since it hasn't been rewarded or consumed yet.
            if (GetProductType(cart) != ProductType.Consumable && purchase.IsAcknowledged())
            {
                return new ConfirmedOrder(cart, orderInfo);
            }

            return new PendingOrder(cart, orderInfo);
        }

        static ProductType GetProductType(ICart cart)
        {
            var cartItem = cart.Items().FirstOrDefault();
            return cartItem?.Product.definition?.type ?? ProductType.Unknown;
        }

        public ICart CreateCartFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            var product = productCache?.Find(purchase.sku) ?? DefaultProduct(purchase);

            var updatedProduct = new Product(product.definition, product.metadata, purchase.receipt)
            {
                transactionID = purchase.purchaseToken
            };

            return new Cart(updatedProduct);
        }

        private Product DefaultProduct(IGooglePurchase purchase)
        {
            var productDescription = purchase.productDescriptions.FirstOrDefault();

            var productId = purchase.sku ?? "";

            return new Product(new ProductDefinition(productId, productId, ProductType.Unknown),
                productDescription?.metadata);
        }
    }
}
