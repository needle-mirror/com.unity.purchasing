#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class FetchStorefrontUseCase : IFetchStorefrontUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal FetchStorefrontUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }

        public void FetchStorefront(Action<AppleStorefront> successCallback, Action<string> errorCallback)
        {
            m_AppleStoreCallbacks.SetFetchStorefrontCallbacks(successCallback, errorCallback);
            m_AppleStoreCallbacks.FetchStorefront();
        }
    }
}
