#nullable enable

using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePurchaseConverter : IGooglePurchaseConverter
    {
        readonly IProductDetailsConverter m_ProductDetailsConverter;

        [Preserve]
        internal GooglePurchaseConverter(IProductDetailsConverter productDetailsConverter)
        {
            m_ProductDetailsConverter = productDetailsConverter;
        }

        public Order CreateOrderFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            var cart = CreateCartFromPurchase(purchase, productCache);
            var orderInfo = new OrderInfo(purchase.receipt, purchase.purchaseToken, GooglePlay.Name);

            if (purchase.IsPending())
            {
                return new DeferredOrder(cart, orderInfo);
            }

            if (purchase.IsAcknowledged())
            {
                return new ConfirmedOrder(cart, orderInfo);
            }

            return new PendingOrder(cart, orderInfo);
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

            return new Product(new ProductDefinition(purchase.sku, purchase.sku, ProductType.Unknown),
                productDescription?.metadata);
        }
    }
}
