#nullable enable

using System;
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
            m_AppleStoreCallbacks.SetRestoreTransactionsCallback(callback);
            m_NativeAppleStore.RestoreTransactions();
        }
    }
}
