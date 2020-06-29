using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Callback interface for <see cref="IStore"/>s.
    /// </summary>
    public interface IStoreCallback
    {
        /// <summary>
        /// For querying product information.
        /// </summary>
        ProductCollection products { get; }

        /// <summary>
        /// Purhasing unavailable.
        /// </summary>
        void OnSetupFailed(InitializationFailureReason reason);

        /// <summary>
        /// Complete setup by providing a list of available products,
        /// complete with metadata and any associated purchase receipts
        /// and transaction IDs.
        ///
        /// Any previously unseen purchases will be completed by the PurchasingManager.
        /// </summary>
        void OnProductsRetrieved(List<ProductDescription> products);

        /// <summary>
        /// Inform Unity Purchasing of a purchase.
        /// </summary>
        void OnPurchaseSucceeded(string storeSpecificId, string receipt, string transactionIdentifier);

        /// <summary>
        /// Notify a failed purchase with associated details.
        /// </summary>
        void OnPurchaseFailed(PurchaseFailureDescription desc);

        /// <summary>
        /// Stores may opt to disable Unity IAP's transaction log if they offer a robust transaction
        /// system of their own (e.g. Apple).
        ///
        /// The default value is 'true'.
        /// </summary>
        bool useTransactionLog { get; set; }
    }
}
