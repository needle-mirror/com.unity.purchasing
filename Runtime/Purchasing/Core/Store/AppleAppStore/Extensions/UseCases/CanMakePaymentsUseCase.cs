#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class CanMakePaymentsUseCase : ICanMakePaymentsUseCase
    {
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal CanMakePaymentsUseCase(INativeAppleStore nativeStore)
        {
            m_NativeAppleStore = nativeStore;
        }

        public bool CanMakePayments()
        {
            return m_NativeAppleStore.canMakePayments;
        }
    }
}
