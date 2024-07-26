#nullable enable

using System.Linq;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Cart Validator that simply checks that the cart itself and the items within are not null.
    /// Also checks that each item within the cart has a non-null product definition.
    /// The other content of the cart items and their format are not checked.
    /// </summary>
    public class NonNullCartValidator : ICartValidator
    {
        /// <summary>
        /// Validates that the cart, its items and product definition are not null.
        /// </summary>
        /// <param name="cart"> The <c>ICart</c> to validate </param>
        /// <exception cref="InvalidCartException"> Thrown if the cart itself or its items list are null. </exception>
        /// <exception cref="InvalidCartItemException"> Thrown if any item within the cart has a null product definition, or if the product or cart item itself is null. </exception>
        public void Validate(ICart cart)
        {
            if (cart == null)
            {
                throw new InvalidCartException("Cart cannot be null.");
            }

            if (cart.Items() == null)
            {
                throw new InvalidCartException("Cart items cannot be null.");
            }

            if (cart.Items().Any(cartItem => cartItem?.Product?.definition == null))
            {
                throw new InvalidCartItemException("Cart cannot contain null items or products.");
            }
        }
    }
}
