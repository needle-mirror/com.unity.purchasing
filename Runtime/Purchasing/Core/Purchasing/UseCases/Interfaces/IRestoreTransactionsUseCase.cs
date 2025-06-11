#nullable enable

using System;

namespace UnityEngine.Purchasing.Interfaces
{
    /// <summary>
    /// An interface for a class that acts out the use case of restoring transactions.
    /// </summary>
    interface IRestoreTransactionsUseCase
    {
        /// <summary>
        /// Initiate a request to a store to restore previously made purchases.
        /// </summary>
        /// <param name="callback">Action will be called when the request to the store comes back.
        /// The bool will be true if it was successful or false if it was not. The string is an optional error message</param>
        void RestoreTransactions(Action<bool, string?>? callback);
    }
}
