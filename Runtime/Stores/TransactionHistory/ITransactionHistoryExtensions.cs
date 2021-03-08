using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// IAP transaction history and debugging extension.
    /// </summary>
    public interface ITransactionHistoryExtensions : IStoreExtension
    {
        PurchaseFailureDescription GetLastPurchaseFailureDescription();

        StoreSpecificPurchaseErrorCode GetLastStoreSpecificPurchaseErrorCode();
    }
}
