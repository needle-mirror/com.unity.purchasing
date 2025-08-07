#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    // TODO: IAP-3929
    class AppleRefreshAppReceiptUseCase : IRefreshAppReceiptUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal AppleRefreshAppReceiptUseCase(IAppleStoreCallbacks appleStoreCallbacks, INativeAppleStore nativeStore)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
            m_NativeAppleStore = nativeStore;
        }

        public void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
            m_AppleStoreCallbacks.SetRefreshAppReceiptCallbacks(successCallback, errorCallback);
            m_NativeAppleStore.RefreshAppReceipt();
        }

        public void SetRefreshAppReceipt(bool refreshAppReceipt)
        {
            m_AppleStoreCallbacks.SetRefreshAppReceipt(refreshAppReceipt);
        }
    }
}
