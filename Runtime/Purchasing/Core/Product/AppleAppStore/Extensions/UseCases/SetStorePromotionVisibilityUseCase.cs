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

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visibility)
        {
            if (product == null)
            {
                var ex = new ArgumentNullException(nameof(product));
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.InvalidProductError, ex);

                throw ex;
            }
            m_NativeAppleStore.SetStorePromotionVisibility(product.definition.storeSpecificId, visibility.ToString());
        }
    }
}
