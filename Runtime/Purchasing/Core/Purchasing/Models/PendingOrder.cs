#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A model representing a pending order of a product.
    /// </summary>
    public class PendingOrder : Order
    {
        /// <summary>
        /// Constructs the pending order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        public PendingOrder(ICart cart, IOrderInfo info) : base(cart, info) { }
    }
}
