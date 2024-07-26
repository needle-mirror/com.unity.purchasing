#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface that allows a cart and its contents to be validated for the purposes of ordering products therein via a store.
    /// </summary>
    public interface ICartValidator
    {
        /// <summary>
        /// Validates a cart, depending on the implementation.
        /// If a cart is invalid, the order is not fit to make purchases for the given store.
        /// </summary>
        /// <param name="cart"> The cart to be validated. </param>
        void Validate(ICart cart);
    }
}
