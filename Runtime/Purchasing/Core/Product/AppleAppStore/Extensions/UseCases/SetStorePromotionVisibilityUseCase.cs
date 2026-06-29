#nullable enable

using System;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class SetStorePromotionVisibilityUseCase : ISetStorePromotionVisibilityUseCase
    {
        readonly INativeAppleStore m_NativeAppleStore;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        [Preserve]
        internal SetStorePromotionVisibilityUseCase(INativeAppleStore nativeStore,
            ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_NativeAppleStore = nativeStore;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public void SetStorePromotionVisibility(string storeSpecificId, AppleStorePromotionVisibility visibility)
        {
            if (string.IsNullOrEmpty(storeSpecificId))
            {
                var ex = new ArgumentNullException(nameof(storeSpecificId));
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.InvalidProductError, ex);

                throw ex;
            }
            m_NativeAppleStore.SetStorePromotionVisibility(storeSpecificId, visibility.ToString());
        }
    }
}
