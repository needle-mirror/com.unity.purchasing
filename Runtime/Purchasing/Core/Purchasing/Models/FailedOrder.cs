namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  A model representing a failed order of a cart.
    /// </summary>
    public class FailedOrder
    {
        internal ICart m_CartOrdered;
        internal PurchaseFailureReason m_FailureReason;
        internal string m_Details;

        /// <summary>
        /// The cart ordered. Read only.
        /// </summary>
        public ICart CartOrdered => m_CartOrdered;

        /// <summary>
        /// The reason the order failed to be purchased. Read only.
        /// </summary>
        public PurchaseFailureReason FailureReason => m_FailureReason;

        /// <summary>
        /// Message containing details about the failed order. Read only.
        /// </summary>
        public string Details => m_Details;

        /// <param name="cart"> The cart ordered.</param>
        /// <param name="reason"> The reason the order failed to be purchased</param>
        /// <param name="details"> Message containing details about the failed order. </param>
        public FailedOrder(ICart cart, PurchaseFailureReason reason, string details = "")
        {
            m_CartOrdered = cart;
            m_FailureReason = reason;
            m_Details = details;
        }
    }
}
