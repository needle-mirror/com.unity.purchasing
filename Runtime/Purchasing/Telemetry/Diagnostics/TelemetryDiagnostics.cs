using System;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryDiagnostics : ITelemetryDiagnostics
    {
        readonly ITelemetryDiagnosticsInstanceWrapper m_TelemetryDiagnosticsInstanceWrapper;

        public TelemetryDiagnostics(ITelemetryDiagnosticsInstanceWrapper telemetryDiagnosticsInstanceWrapper)
        {
            m_TelemetryDiagnosticsInstanceWrapper = telemetryDiagnosticsInstanceWrapper;
        }

        public void SendDiagnostic(string diagnosticName, Exception e)
        {
            m_TelemetryDiagnosticsInstanceWrapper.SendDiagnostic(diagnosticName, e.ToString());
        }
    }
}
