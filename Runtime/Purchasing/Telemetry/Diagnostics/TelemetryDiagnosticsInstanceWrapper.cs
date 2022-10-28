using System;
using Uniject;
using Unity.Services.Core.Telemetry.Internal;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryDiagnosticsInstanceWrapper : ITelemetryDiagnosticsInstanceWrapper
    {
        IDiagnostics m_Instance;
        ILogger m_Logger;
        IUtil m_Util;

        readonly TelemetryQueue<TelemetryDiagnosticParams> m_Queue;

        public TelemetryDiagnosticsInstanceWrapper(ILogger logger, IUtil util)
        {
            m_Logger = logger;
            m_Util = util;
            m_Queue = new TelemetryQueue<TelemetryDiagnosticParams>(SendDiagnosticOnMainThread);
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
                SendDiagnosticOnMainThread(diagnosticParams);
            }
            else
            {
                m_Queue.QueueEvent(diagnosticParams);
            }
        }

        void SendDiagnosticOnMainThread(TelemetryDiagnosticParams diagnosticParams)
        {
            m_Util.RunOnMainThread(() => SendDiagnosticAndCatchExceptions(diagnosticParams));
        }

        void SendDiagnosticAndCatchExceptions(TelemetryDiagnosticParams diagnosticParams)
        {
            try
            {
                m_Instance.SendDiagnostic(diagnosticParams.name, diagnosticParams.exception);
            }
            catch (Exception exception)
            {
                m_Logger.LogIAPError($"An exception occurred when sending diagnostic {diagnosticParams.name}. Message: {exception.Message}");
            }
        }
    }
}
