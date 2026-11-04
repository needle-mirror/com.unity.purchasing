#nullable enable

using System;
using Purchasing.Utilities;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class AppleRestoreTransactionsUseCase : IRestoreTransactionsUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal AppleRestoreTransactionsUseCase(IAppleStoreCallbacks appleStoreCallbacks, INativeAppleStore nativeStore)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
            m_NativeAppleStore = nativeStore;
        }

        public void RestoreTransactions(Action<bool, string?>? callback)
        {
            // StoreKit 1 reports restore completion through the transaction observer, which starts only once
            // products are fetched and OnPurchasePending has a subscriber. Without it the restore never completes.
            if (StoreKitSelector.UseStoreKit1() && !m_AppleStoreCallbacks.IsTransactionObserverEnabled)
            {
                callback?.Invoke(false, "StoreKit 1 restores need fetched products and a subscriber to OnPurchasePending. Subscribe to OnPurchasePending before calling RestoreTransactions.");
                return;
            }

            m_AppleStoreCallbacks.SetRestoreTransactionsCallback(callback);
            m_NativeAppleStore.RestoreTransactions();
        }
    }
}
