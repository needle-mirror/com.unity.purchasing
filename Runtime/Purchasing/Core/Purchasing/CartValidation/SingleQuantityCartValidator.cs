#nullable enable

using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A cart validator that checks that the quantity of each product in the cart is exactly one.
    /// </summary>
    public class SingleQuantityCartValidator : ICartValidator
    {
        /// <summary>
        /// Validates that the cart's items each have a quantity of exactly 1.
        /// </summary>
        /// <param name="cart"> The cart to be validated.</param>
        /// <exception cref="InvalidCartItemException"> Thrown if any of the products in the cart have a non-1 quantity. </exception>
        public void Validate(ICart cart)
        {
            if (cart.Items().Any(cartItem => cartItem.Quantity != 1))
            {
                throw new InvalidCartItemException("Cart item quantity should be equal to 1.");
            }
        }
    }
}
