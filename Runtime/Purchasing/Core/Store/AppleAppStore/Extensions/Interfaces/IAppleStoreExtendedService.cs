using System;
using UnityEngine.Purchasing.UseCases.Interfaces;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Apple Store store service extension.
    /// </summary>
    public interface IAppleStoreExtendedService : IStoreServiceExtension
    {
        /// <summary>
        /// Determine if the user can make payments.
        /// </summary>
        bool canMakePayments { get; }

        /// <summary>
        /// Sets an obfuscation string for tracking user purchases.
        /// For more information, see <a href="https://developer.apple.com/documentation/storekit/transaction/appaccounttoken">appAccountToken documentation</a>.
        /// </summary>
        /// <param name="appAccountToken">The obfuscated account token</param>
        void SetAppAccountToken(Guid appAccountToken);

        /// <summary>
        /// Clear all persistent data from the transaction log.
        /// Available in debug only.
        /// </summary>
        void ClearTransactionLog();
    }
}
