namespace UnityEngine.Purchasing.Telemetry
{
    struct TelemetryDiagnosticParams
    {
        internal string name;
        internal string exception;
        internal TelemetryDiagnosticParams(string diagnosticName, string diagnosticException)
        {
            name = diagnosticName;
            exception = diagnosticException;
        }
    }
}
