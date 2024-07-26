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
        /// <param name="product"> The product. </param>
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


        [Obsolete("Use item.Product instead")]
        /// <summary>
        /// The product.
        /// </summary>
        public Product product => item.Product;


        /// <summary>
        /// The reason for the failure.
        /// </summary>
        public PurchaseFailureReason reason { get; private set; }

        /// <summary>
        /// The message containing details about the failed purchase.
        /// </summary>
        public string message { get; private set; }

        internal FailedOrder ConvertToFailedOrder()
        {
            var cart = new Cart(item);
            return new FailedOrder(cart, reason, message);
        }
    }
}
