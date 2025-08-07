namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A model representing a deferred subscription change order.
    /// This is a planned order that has not yet been payed for and that will occur at a certain date in the future.
    /// </summary>
    public class DeferredPaymentUntilRenewalDateOrder
    {
        /// <summary>
        /// The Order containing the subscription currently owned by the player. Read only.
        /// </summary>
        public Order CurrentOrder { get; }

        /// <summary>
        /// The subscription ordered. Read only.
        /// </summary>
        public Product SubscriptionOrdered { get; }

        /// <summary>
        /// Creates a new instance of <see cref="DeferredPaymentUntilRenewalDateOrder"/>.
        /// </summary>
        /// <param name="currentOrder">The current order containing the subscription owned by the player.</param>
        /// <param name="subscriptionOrdered">The subscription that the player has ordered to be deferred until the renewal date.</param>
        public DeferredPaymentUntilRenewalDateOrder(Order currentOrder, Product subscriptionOrdered)
        {
            CurrentOrder = currentOrder;
            SubscriptionOrdered = subscriptionOrdered;
        }
    }
}
