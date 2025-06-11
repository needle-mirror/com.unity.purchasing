namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for a class that handles callbacks for checking entitlements from a Store.
    /// </summary>
    public interface IStoreCheckEntitlementCallback
    {
        /// <summary>
        /// Informs Unity Purchasing of a product's entitlement status result.
        /// Always invoked, regardless of success or failure.
        /// </summary>
        /// <param name="productDefinition">The product being checked.</param>
        /// <param name="status">The result of the entitlement check.</param>
        /// <param name="message">Optional error or status detail message.</param>
        void OnCheckEntitlement(ProductDefinition productDefinition, EntitlementStatus status, string message = null);

    }
}
