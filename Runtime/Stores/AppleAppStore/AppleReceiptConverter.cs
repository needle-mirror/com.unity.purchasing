#nullable enable

using System;
using UnityEngine.Purchasing.Security;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class AppleReceiptConverter : IAppleReceiptConverter
    {
        private readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        [Preserve]
        internal AppleReceiptConverter(ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public AppleReceipt? ConvertFromBase64String(string? receipt)
        {
            if (!string.IsNullOrEmpty(receipt))
            {
                var parser = new AppleReceiptParser();
                try
                {
                    return parser.Parse(Convert.FromBase64String(receipt));
                }
                catch (Exception ex)
                {
                    m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.ParseReceiptTransactionError, ex);
                }
            }

            return null;
        }
    }
}
