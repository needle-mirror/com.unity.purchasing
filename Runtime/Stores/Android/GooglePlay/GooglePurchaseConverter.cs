#nullable enable

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
            return cartItem?.Product.type ?? ProductType.Unknown;
        }

        public ICart CreateCartFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            var product = productCache?.Find(purchase.sku) ?? DefaultProduct(purchase);

            // Multi-listing aware: pick the listing whose storeSpecificId matches the actual SKU
            // purchased. Falls back to the base listing in the single-listing case (where they're equal).
            var sourceListing = productCache?.FindCatalogListingByStoreSpecificId(purchase.sku) ?? product.baseListing;

// Obsolete: Product(ProductDefinition, ProductMetadata, string), Product.transactionID
#pragma warning disable 618, 612
            var updatedProduct = new Product(sourceListing?.definition, sourceListing?.metadata, purchase.receipt)
            {
                transactionID = purchase.purchaseToken
            };
#pragma warning restore 618, 612

            // The new Product's only listing is keyed by definition.catalogListingId, which may
            // differ from updatedProduct.uSku for non-base listings. Use the explicit-listing
            // CartItem ctor so we don't rely on baseListing being populated.
            var catalogListingId = sourceListing?.definition?.catalogListingId;
            return catalogListingId != null && updatedProduct.catalogListings.ContainsKey(catalogListingId)
                ? new Cart(new CartItem(updatedProduct, catalogListingId))
                : new Cart(updatedProduct);
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
