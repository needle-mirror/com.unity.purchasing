#nullable enable

using System;
namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  A model representing a failed order of a cart.
    /// </summary>
    public class FailedOrder : Order
    {
        PurchaseFailureReason m_FailureReason;
        string m_Details;

        /// <summary>
        /// The reason the order failed to be purchased.
        /// </summary>
        public PurchaseFailureReason FailureReason => m_FailureReason;

        /// <summary>
        /// Message containing details about the failed order. Read only.
        /// </summary>
        public string Details => m_Details;

        /// <summary>
        /// Creates a new FailedOrder with empty OrderInfo.
        /// </summary>
        /// <param name="cart">The cart ordered.</param>
        /// <param name="reason">The reason the order failed to be purchased.</param>
        /// <param name="details">The message containing details about the failure.</param>
        public FailedOrder(ICart cart, PurchaseFailureReason reason, string details)
            : base(cart, new OrderInfo(string.Empty, string.Empty, string.Empty))
        {
            m_FailureReason = reason;
            m_Details = details;
        }

        /// <summary>
        /// Creates a new FailedOrder from an existing order.
        /// </summary>
        /// <param name="order">The original order that failed.</param>
        /// <param name="reason">The reason the order failed.</param>
        /// <param name="details">The message containing details about the failure.</param>
        public FailedOrder(Order order, PurchaseFailureReason reason, string details)
            : base(order.CartOrdered, order.Info)
        {
            m_FailureReason = reason;
            m_Details = details;
        }
    }
}
