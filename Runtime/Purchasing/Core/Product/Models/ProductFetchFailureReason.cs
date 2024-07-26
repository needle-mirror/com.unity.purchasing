namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The various reasons a product fetch can fail.
    /// </summary>
    public enum ProductFetchFailureReason
    {
        /// <summary>
        /// The provider of the products may be unavailable.
        /// </summary>
        ProviderUnavailable = 0,

        /// <summary>
        /// The products requested was reported unavailable by the purchasing system.
        /// </summary>
        ProductsUnavailable = 1,

        /// <summary>
        /// A catch all for remaining purchase problems.
        /// </summary>
        Unknown = 2
    }
}
