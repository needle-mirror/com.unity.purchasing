#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The base Model encapsulating an order to purchase a cart.
    /// </summary>
    public abstract class Order
    {
        /// <summary>
        /// The product ordered. Read only.
        /// </summary>
        public ICart CartOrdered { get; internal set; }

        /// <summary>
        /// Additional information about the order
        /// </summary>
        public IOrderInfo Info { get; }


        /// <summary>
        /// Constructs the base order object.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="info">Additional information concerning this order.</param>
        protected Order(ICart cart, IOrderInfo info)
        {
            CartOrdered = cart;
            Info = info;
        }
    }
}
