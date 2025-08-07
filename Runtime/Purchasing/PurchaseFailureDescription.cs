#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a failed purchase as described
    /// by a purchasing service.
    /// </summary>
    public class PurchaseFailureDescription
    {
        /// <summary>
        /// Parametrized Constructor.
        /// </summary>
        /// <param name="item"> The cart item. </param>
        /// <param name="reason"> The reason for the purchase failure </param>
        /// <param name="message"> The message containing details about the failed purchase. </param>
        public PurchaseFailureDescription(CartItem item, PurchaseFailureReason reason, string message)
        {
            this.item = item;
            this.reason = reason;
            this.message = message;
        }

        /// <summary>
        /// The cart item wrapping the product and quantity.
        /// </summary>
        public CartItem item { get; private set; }


        /// <summary>
        /// The product.
        /// </summary>
        [Obsolete("Use item.Product instead")]
        public Product product => item.Product;


        /// <summary>
        /// The reason for the failure.
        /// </summary>
        public PurchaseFailureReason reason { get; private set; }

        /// <summary>
        /// The message containing details about the failed purchase.
        /// </summary>
        public string message { get; private set; }

        /// <summary>
        /// Converts a purchase failure description to a FailedOrder object.
        /// </summary>
        /// <param name="transactionId">The transaction ID (e.g., purchase token in Google Play)
        /// associated with this failed order. If null, an empty order info will be created.</param>
        /// <returns>A FailedOrder containing the purchase failure information and transaction ID if provided.</returns>
        /// <remarks>
        /// The transaction ID is crucial for matching failed orders with their original purchase requests,
        /// especially in systems that need to track or confirm specific transactions.
        /// </remarks>
        internal FailedOrder ConvertToFailedOrder(string? transactionId = "")
        {
            var cart = new Cart(item);
            var orderInfo = new OrderInfo(string.Empty, transactionId, string.Empty);
            var pendingOrder = new PendingOrder(cart, orderInfo);
            return new FailedOrder(pendingOrder, reason, message);
        }
    }
}
