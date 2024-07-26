namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An object describing the Entitlement checked for a given Product.
    /// </summary>
    public class Entitlement
    {

        /// <summary>
        /// The Product Checked for Entitlement
        /// </summary>
        public Product ProductChecked { get; }

        /// <summary>
        /// The Order found for the Entitled Product
        /// null is the Product is not entitled
        /// A Pending Order if the entitlement needs to be Confirmed via ConfirmOrder
        /// A Confirmed Order if th entitlement is complete
        /// </summary>
        public Order EntitlementOrder { get; }

        /// <summary>
        /// The status of entitlement.
        /// </summary>
        public EntitlementStatus Status { get; }

        internal Entitlement(Product product, Order order, EntitlementStatus status)
        {
            ProductChecked = product;
            EntitlementOrder = order;
            Status = status;
        }
    }
}
