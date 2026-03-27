#nullable enable

using Uniject;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.GoogleBilling.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.GoogleBilling
{
    internal class BillingClientFactory : IBillingClientFactory
    {
        internal static BillingClientFactory? s_Instance;
        internal IUtil m_Util { get; }
        internal ITelemetryDiagnostics m_TelemetryDiagnostics { get; }

        BillingClientFactory(IUtil util, ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        internal static BillingClientFactory Instance()
        {
            if (s_Instance == null)
            {
                var logger = Debug.unityLogger;
                var util = UnityUtilContainer.Instance();
                var telemetryDiagnosticsWrapper = new TelemetryDiagnosticsInstanceWrapper(logger, util);
                var telemetryDiagnostics = new TelemetryDiagnostics(telemetryDiagnosticsWrapper);
                s_Instance = new BillingClientFactory(util, telemetryDiagnostics);
            }

            return s_Instance;
        }

        public IExternalBillingProgramClientInternal CreateExternalBillingProgramClient()
        {
            return new ExternalBillingProgramClientInternal(m_Util, m_TelemetryDiagnostics);
        }
    }
}
