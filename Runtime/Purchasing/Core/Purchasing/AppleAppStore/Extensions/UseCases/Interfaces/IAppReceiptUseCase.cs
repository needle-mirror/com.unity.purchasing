#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of reading the App Receipt.
    /// </summary>
    interface IAppReceiptUseCase
    {
        /// <summary>
        /// Read the App Receipt from local storage.
        /// Returns null for iOS less than or equal to 6, may also be null on a reinstalling and require refreshing.
        /// </summary>
        string? AppReceipt();
    }
}
