#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class PresentCodeRedemptionSheetUseCase : IPresentCodeRedemptionSheetUseCase
    {
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal PresentCodeRedemptionSheetUseCase(INativeAppleStore nativeStore)
        {
            m_NativeAppleStore = nativeStore;
        }

        public void PresentCodeRedemptionSheet()
        {
            m_NativeAppleStore.PresentCodeRedemptionSheet();
        }
    }
}
