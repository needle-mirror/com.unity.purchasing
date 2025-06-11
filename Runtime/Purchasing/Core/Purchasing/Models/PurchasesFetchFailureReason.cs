namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The various reasons a purchases fetch can fail.
    /// </summary>
    public enum PurchasesFetchFailureReason
    {
        /// <summary>
        /// Purchasing may be disabled in security settings.
        /// </summary>
        PurchasingUnavailable,

        /// <summary>
        /// The purchases couldn't be fetched because the store is not connected.
        /// Use IStoreService.Connect() to initialize the connection to the store.
        /// </summary>
        StoreNotConnected,

        /// <summary>
        /// A catch all for remaining purchase problems.
        /// </summary>
        Unknown
    }
}
