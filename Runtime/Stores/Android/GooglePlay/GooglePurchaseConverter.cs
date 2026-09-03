#nullable enable

using System.Linq;
using System.Threading.Tasks;
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

        public async Task<Order> CreateOrderFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            var cart = await CreateCartFromPurchase(purchase, productCache);
            var orderInfo = new GoogleOrderInfo(purchase.receipt, purchase.purchaseToken, GooglePlay.Name, purchase.obfuscatedAccountId, purchase.obfuscatedProfileId, purchase.orderId);

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

        public async Task<ICart> CreateCartFromPurchase(IGooglePurchase purchase, IProductCache? productCache)
        {
            // Cache-by-sku → backend reverse-lookup → cache-by-uSku → DefaultProduct.
            // DefaultProduct is the terminal fallback because it enriches the returned Product with
            // Google's productDescription metadata (title / price / currency) — richer than a plain
            // CreateUnknownProduct. When the resolver produced a match, we pass its uSku + type
            // through so the fallback Product's ProductDefinition carries both ids and the correct
            // ProductType.
            var product = productCache?.Find(purchase.sku);
            ResolvedUSku? resolved = null;
            if (product == null && productCache != null)
            {
                resolved = await productCache.ResolveByStoreSpecificIdAsync(purchase.sku);
                if (resolved != null && !string.IsNullOrEmpty(resolved.USku))
                {
                    product = productCache.Find(resolved.USku);
                }
            }
            product ??= DefaultProduct(purchase, resolved?.USku ?? purchase.sku ?? "", resolved?.Type ?? ProductType.Unknown);

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

        // Terminal fallback when neither the local cache nor the backend reverse lookup can
        // identify the product. Metadata comes from the Google productDescription attached to this
        // purchase, which is richer than a bare CreateUnknownProduct. Callers pick the uSku:
        // the backend-resolved value when available, otherwise the raw Google sku.
        Product DefaultProduct(IGooglePurchase purchase, string uSku, ProductType type)
        {
            var productDescription = purchase.productDescriptions.FirstOrDefault();
            return new Product(new ProductDefinition(uSku, purchase.sku ?? "", type),
                productDescription?.metadata);
        }
    }
}
