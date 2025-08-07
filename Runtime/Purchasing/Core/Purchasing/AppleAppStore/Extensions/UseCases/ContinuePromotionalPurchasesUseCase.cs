#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class ContinuePromotionalPurchasesUseCase : IContinuePromotionalPurchasesUseCase
    {
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal ContinuePromotionalPurchasesUseCase(INativeAppleStore nativeStore)
        {
            m_NativeAppleStore = nativeStore;
        }

        public void ContinuePromotionalPurchases()
        {
            m_NativeAppleStore.ContinuePromotionalPurchases();
        }
    }
}
