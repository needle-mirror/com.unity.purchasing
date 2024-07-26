#nullable enable

using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A cart validator that checks that only one product is in the cart items list.
    /// </summary>
    public class SingleProductCartValidator : ICartValidator
    {
        /// <summary>
        /// Validates that there is exactly one item in the cart.
        /// </summary>
        /// <param name="cart"> The cart to be validated. </param>
        /// <exception cref="InvalidCartException"> Thrown if the amount of items in the cart is not 1. </exception>
        public void Validate(ICart cart)
        {
            if (cart.Items().Count() != 1)
            {
                throw new InvalidCartException("Cart must contain 1 cart item.");
            }
        }
    }
}
