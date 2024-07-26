#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class AppReceiptUseCase : IAppReceiptUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal AppReceiptUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }

        public string? AppReceipt()
        {
            return m_AppleStoreCallbacks.GetAppReceipt();
        }
    }
}
