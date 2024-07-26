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
        /// A catch all for remaining purchase problems.
        /// </summary>
        Unknown
    }
}
