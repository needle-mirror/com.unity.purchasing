#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class SetAppAccountTokenUseCase : ISetAppAccountTokenUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal SetAppAccountTokenUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }
        public void SetAppAccountToken(Guid token)
        {
            m_AppleStoreCallbacks.SetAppAccountToken(token);
        }

    }
}
