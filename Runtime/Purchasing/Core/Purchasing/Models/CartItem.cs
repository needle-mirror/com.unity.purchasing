#nullable enable

namespace UnityEngine.Purchasing
{
    public class CartItem
    {
        public Product Product { get; }
        public int Quantity { get; }

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

        public static implicit operator CartItem(Product product)
        {
            return new CartItem(product);
        }

        public override int GetHashCode()
        {
            return Product.GetHashCode();
        }

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
