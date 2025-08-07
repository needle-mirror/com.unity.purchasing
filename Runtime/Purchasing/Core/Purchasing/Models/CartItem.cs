#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents an item in a shopping cart, containing a product and its quantity.
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// The product associated with this cart item.
        /// </summary>
        public Product Product { get; }
        /// <summary>
        /// The quantity of the product in the cart item.
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CartItem"/> class with a specified product and a quantity of 1.
        /// </summary>
        /// <param name="product">The product to be added to the cart.</param>
        public CartItem(Product product) : this(product, 1) { }

        internal CartItem(Product product, int quantity)
        {
            if (product?.definition == null)
            {
                throw new InvalidCartItemException("A cart item product and product definition must not be null.");
            }

            Product = product;
            Quantity = quantity;
        }

        /// <summary>
        /// Implicitly converts a <see cref="Product"/> to a <see cref="CartItem"/> with a quantity of 1.
        /// </summary>
        /// <param name="product">The product to convert.</param>
        /// <returns>A new <see cref="CartItem"/> instance with the specified product and a quantity of 1.</returns>
        public static implicit operator CartItem(Product product)
        {
            return new CartItem(product);
        }

        /// <summary>
        /// Returns a hash code for this instance, which is based on the product's hash code.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return Product.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="CartItem"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is a <see cref="CartItem"/> with the same product; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CartItem cartItem)
            {
                return Product.Equals(cartItem.Product);
            }

            return false;
        }
    }
}
