using System;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryDiagnostics : ITelemetryDiagnostics
    {
        readonly ITelemetryDiagnosticsInstanceWrapper m_TelemetryDiagnosticsInstanceWrapper;

        [Preserve]
        internal TelemetryDiagnostics(ITelemetryDiagnosticsInstanceWrapper telemetryDiagnosticsInstanceWrapper)
        {
            m_TelemetryDiagnosticsInstanceWrapper = telemetryDiagnosticsInstanceWrapper;
        }

        public void SendDiagnostic(string diagnosticName, Exception e)
        {
            try
            {
                m_TelemetryDiagnosticsInstanceWrapper.SendDiagnostic(diagnosticName, e.ToString());
            }
            catch (IapTelemetryException exception)
            {
                Debug.unityLogger.LogIAPError($"An exception occured while sending a diagnostic: {exception.Message}");
            }
        }
    }
}
