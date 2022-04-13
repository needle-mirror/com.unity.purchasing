using System;
using Unity.Services.Core.Telemetry.Internal;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryDiagnosticsInstanceWrapper : ITelemetryDiagnosticsInstanceWrapper
    {
        IDiagnostics m_Instance;
        TelemetryQueue<TelemetryDiagnosticParams> m_Queue;

        public TelemetryDiagnosticsInstanceWrapper()
        {
            m_Queue = new TelemetryQueue<TelemetryDiagnosticParams>(SendDiagnostic);
        }

        public void SetDiagnosticsInstance(IDiagnostics diagnosticsInstance)
        {
            m_Instance = diagnosticsInstance;
            m_Queue.SendQueuedEvents();
        }

        public void SendDiagnostic(string diagnosticName, string diagnosticException)
        {
            var diagnosticParams = new TelemetryDiagnosticParams(diagnosticName, diagnosticException);
            if (m_Instance != null)
            {
                SendDiagnostic(diagnosticParams);
            }
            else
            {
                m_Queue.QueueEvent(diagnosticParams);
            }
        }

        void SendDiagnostic(TelemetryDiagnosticParams diagnosticParams)
        {
            m_Instance.SendDiagnostic(diagnosticParams.name, diagnosticParams.exception);
        }
    }
}
