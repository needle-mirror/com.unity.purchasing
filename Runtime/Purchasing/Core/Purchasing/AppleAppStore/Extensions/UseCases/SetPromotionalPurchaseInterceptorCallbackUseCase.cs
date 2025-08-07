#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class SetPromotionalPurchaseInterceptorCallbackUseCase : ISetPromotionalPurchaseInterceptorCallbackUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal SetPromotionalPurchaseInterceptorCallbackUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }

        public event Action<Product>? OnPromotionalPurchaseIntercepted
        {
            add => m_AppleStoreCallbacks.OnPromotionalPurchaseIntercepted += value;
            remove => m_AppleStoreCallbacks.OnPromotionalPurchaseIntercepted -= value;
        }
    }
}
