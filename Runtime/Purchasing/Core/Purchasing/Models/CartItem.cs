#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents an item in a shopping cart, containing a product, the id of the specific
    /// catalog listing being purchased, and a quantity.
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// The product associated with this cart item.
        /// </summary>
        public Product Product { get; }

        /// <summary>
        /// The id of the catalog listing being purchased. Defaults to <see cref="Product.uSku"/>
        /// (the base listing) when constructed from a <see cref="Product"/> alone. The actual
        /// listing can be retrieved via <c>Product.catalogListings[CatalogListingId]</c>.
        /// </summary>
        public string CatalogListingId { get; }

        /// <summary>
        /// The quantity of the product in the cart item.
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CartItem"/> class with a specified product and a quantity of 1.
        /// The product's base listing id is used as the <see cref="CatalogListingId"/>.
        /// </summary>
        /// <param name="product">The product to be added to the cart.</param>
        public CartItem(Product product) : this(product, 1) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CartItem"/> class with a specified product and quantity.
        /// The product's base listing id is used as the <see cref="CatalogListingId"/>.
        /// </summary>
        /// <param name="product">The product to be added to the cart.</param>
        /// <param name="quantity">The quantity to be added to the cart.</param>
        /// <exception cref="InvalidCartItemException">Thrown when the product is null or has no catalog listing whose id matches <see cref="Product.uSku"/>.</exception>
        public CartItem(Product product, int quantity)
        {
            if (product?.uSku == null || product.baseListing?.id == null)
            {
                throw new InvalidCartItemException("A cart item product must have a catalog listing.");
            }

            Product = product;
            CatalogListingId = product.baseListing.id;
            Quantity = quantity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CartItem"/> class targeting a specific
        /// catalog listing on the product.
        /// </summary>
        /// <param name="product">The product to be added to the cart.</param>
        /// <param name="catalogListingId">The id of the catalog listing on the product to purchase.</param>
        /// <param name="quantity">The quantity to be added to the cart.</param>
        /// <exception cref="InvalidCartItemException">Thrown when the product is null or has no listing matching <paramref name="catalogListingId"/>.</exception>
        public CartItem(Product product, string catalogListingId, int quantity = 1)
        {
            if (product == null || catalogListingId == null || !product.catalogListings.ContainsKey(catalogListingId))
            {
                var have = product?.catalogListings == null
                    ? "(product or catalogListings was null)"
                    : product.catalogListings.Count == 0
                        ? "(empty)"
                        : string.Join(", ", product.catalogListings.Keys);
                throw new InvalidCartItemException(
                    $"No catalog listing '{catalogListingId}' found on the given product. " +
                    $"Product uSku='{product?.uSku ?? "(null)"}'. " +
                    $"Available listing ids on this product: [{have}]");
            }

            Product = product;
            CatalogListingId = catalogListingId;
            Quantity = quantity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CartItem"/> class targeting a specific
        /// catalog listing on the product.
        /// </summary>
        /// <param name="product">The product to be added to the cart.</param>
        /// <param name="catalogListing">The catalog listing on the product to purchase.</param>
        /// <param name="quantity">The quantity to be added to the cart.</param>
        public CartItem(Product product, CatalogListing catalogListing, int quantity = 1)
            : this(product, catalogListing?.id!, quantity) { }

        /// <summary>
        /// Implicitly converts a <see cref="Product"/> to a <see cref="CartItem"/> with a quantity of 1.
        /// The product's base listing id is used as the <see cref="CatalogListingId"/>.
        /// </summary>
        /// <param name="product">The product to convert.</param>
        /// <returns>A new <see cref="CartItem"/> instance with the specified product and a quantity of 1.</returns>
        public static implicit operator CartItem(Product product)
        {
            return new CartItem(product);
        }

        /// <summary>
        /// Returns a hash code for this instance, combining the product and the targeted catalog listing id.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (Product?.GetHashCode() ?? 0);
                hash = hash * 31 + (CatalogListingId?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="CartItem"/>.
        /// Two cart items are equal when they reference the same product and the same catalog listing id.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is a <see cref="CartItem"/> with the same product and catalog listing id; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CartItem cartItem)
            {
                return Product.Equals(cartItem.Product) && CatalogListingId == cartItem.CatalogListingId;
            }

            return false;
        }
    }
}
