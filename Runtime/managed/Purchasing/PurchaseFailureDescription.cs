using System;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Represents a failed purchase as described
    /// by a purchasing service.
    /// </summary>
    public class PurchaseFailureDescription
    {
        public PurchaseFailureDescription(string productId, PurchaseFailureReason reason, string message)
        {
            this.productId = productId;
            this.reason = reason;
            this.message = message;
        }

        /// <summary>
        /// The store specific product ID.
        /// </summary>
        public string productId { get; private set; }

        public PurchaseFailureReason reason { get; private set; }
        public String message { get; private set; }
    }
}
