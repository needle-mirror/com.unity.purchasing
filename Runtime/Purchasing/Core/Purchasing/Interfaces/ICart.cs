using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a shopping cart that contains items to be purchased.
    /// </summary>
    public interface ICart
    {
        /// <summary>
        /// Gets the <see cref="CartItem"/>s in the cart.
        /// </summary>
        /// <returns>A read-only list of <see cref="CartItem"/>s.</returns>
        IReadOnlyList<CartItem> Items();
    }
}
