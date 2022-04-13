using System;

namespace UnityEngine.Purchasing.Telemetry
{
    interface ITelemetryDiagnostics
    {
        void SendDiagnostic(string diagnosticName, Exception e);
    }
}
