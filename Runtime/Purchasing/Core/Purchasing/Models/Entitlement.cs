#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An object describing the Entitlement checked for a given Product.
    /// </summary>
    public class Entitlement
    {

        /// <summary>
        /// The product being checked for entitlement. May be <c>null</c> if the entitlement check failed early.
        /// </summary>
        public Product? Product { get; }

        /// <summary>
        /// Returns the Order associated with the entitled product.
        /// The Order will be a PendingOrder if the purchase needs to be confirmed via ConfirmPurchase,
        /// otherwise, the Order will be a ConfirmedOrder if the purchase has been confirmed.
        /// If the product is not entitled, the Order will be null.
        /// </summary>
        public Order? Order { get; internal set; }

        /// <summary>
        /// The status of entitlement.
        /// </summary>
        public EntitlementStatus Status { get; }

        /// <summary>
        /// Optional message describing the entitlement result. Can be <c>null</c> if no message is provided.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Represents the entitlement state of a product, along with any associated order and message.
        /// </summary>
        /// <param name="product">The product being checked. Can be <c>null</c> in error scenarios.</param>
        /// <param name="order">The associated order, if any.</param>
        /// <param name="status">The entitlement status.</param>
        /// <param name="message">Optional message providing context about the entitlement status.</param>
        internal Entitlement(Product? product, Order? order, EntitlementStatus status, string? message = null)
        {
            Product = product;
            Order = order;
            Status = status;
            ErrorMessage = message;
        }
    }
}
