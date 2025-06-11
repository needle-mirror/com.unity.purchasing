#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A model representing a deferred order of a product.
    ///
    /// Deferred orders are not paid for so they do not have a `transactionID`.
    /// </summary>
    public class DeferredOrder : Order
    {
        /// <summary>
        /// Constructs the deferred order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        public DeferredOrder(ICart cart, IOrderInfo info) : base(cart, info) { }
    }
}
