namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Implemented by Application developers using Unity Purchasing.
    /// </summary>
    public interface IStoreListener
    {
        /// <summary>
        /// Purchasing failed to initialise for a non recoverable reason.
        /// </summary>
        void OnInitializeFailed(InitializationFailureReason error);

        /// <summary>
        /// A purchase succeeded.
        /// </summary>
        PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e);

        /// <summary>
        /// A purchase failed with specified reason.
        /// </summary>
        void OnPurchaseFailed(Product i, PurchaseFailureReason p);

        /// <summary>
        /// Purchasing initialized successfully.
        ///
        /// The <c>IStoreController</c> is available for accessing
        /// purchasing functionality.
        /// </summary>
        void OnInitialized(IStoreController controller, IExtensionProvider extensions);
    }
}
