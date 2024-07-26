namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for a class that handles callbacks for checking entitlements from a Store.
    /// </summary>
    public interface IStoreCheckEntitlementCallback
    {
        /// <summary>
        /// Inform Unity Purchasing of a product's entitlement status.
        /// </summary>
        /// <param name="productDefinition">The product to check for.</param>
        /// <param name="isEntitled">The entitlement status of the product.</param>
        void OnCheckEntitlementSucceeded(ProductDefinition productDefinition, EntitlementStatus status);
    }
}
