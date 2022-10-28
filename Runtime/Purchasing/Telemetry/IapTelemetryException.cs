using System;

namespace UnityEngine.Purchasing.Telemetry
{
    class IapTelemetryException : Exception
    {
        public IapTelemetryException() { }

        public IapTelemetryException(string message)
            : base(message) { }

        public IapTelemetryException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
