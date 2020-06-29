using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Used by Applications to control Unity Purchasing.
    /// </summary>
    public interface IStoreController
    {
        ProductCollection products { get; }
        void InitiatePurchase(Product product, string payload);
        void InitiatePurchase(string productId, string payload);
        void InitiatePurchase(Product product);
        void InitiatePurchase(string productId);
        void FetchAdditionalProducts(HashSet<ProductDefinition> products, Action successCallback,
            Action<InitializationFailureReason> failCallback);

        /// <summary>
        /// Where an Application returned ProcessingResult.Pending
        /// from IStoreListener.ProcessPurchase(), Applications should call
        /// this method when processing completes.
        /// </summary>
        void ConfirmPendingPurchase(Product product);
    }
}
