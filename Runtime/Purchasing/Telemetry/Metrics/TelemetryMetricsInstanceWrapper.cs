using System;
using Uniject;
using Unity.Services.Core.Telemetry.Internal;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryMetricsInstanceWrapper : ITelemetryMetricsInstanceWrapper
    {
        IMetrics m_Instance;
        ILogger m_Logger;
        IUtil m_Util;
        readonly TelemetryQueue<TelemetryMetricParams> m_Queue;

        public TelemetryMetricsInstanceWrapper(ILogger logger, IUtil util)
        {
            m_Logger = logger;
            m_Util = util;
            m_Queue = new TelemetryQueue<TelemetryMetricParams>(SendMetricOnMainThread);
        }

        public void SetMetricsInstance(IMetrics metricsInstance)
        {
            m_Instance = metricsInstance;
            if (m_Instance != null)
            {
                m_Queue.SendQueuedEvents();
            }
        }

        public void SendMetric(TelemetryMetricTypes metricType, string metricName, double metricTimeSeconds)
        {
            var telemetryMetricParams = new TelemetryMetricParams(metricType, metricName, metricTimeSeconds);
            if (m_Instance != null)
            {
                SendMetricOnMainThread(telemetryMetricParams);
            }
            else
            {
                m_Queue.QueueEvent(telemetryMetricParams);
            }
        }

        void SendMetricOnMainThread(TelemetryMetricParams metricParams)
        {
            m_Util.RunOnMainThread(() => SendMetricByTypeAndCatchExceptions(metricParams));
        }

        void SendMetricByTypeAndCatchExceptions(TelemetryMetricParams metricParams)
        {
            try
            {
                SendMetricByType(metricParams);
            }
            catch (Exception exception)
            {
                m_Logger.LogIAPError($"An exception occurred when sending metric {metricParams.name} of type {metricParams.type}. Message: {exception.Message}");
            }
        }

        void SendMetricByType(TelemetryMetricParams metricParams)
        {
            switch (metricParams.type)
            {
                case TelemetryMetricTypes.Gauge:
                    m_Instance.SendGaugeMetric(metricParams.name, metricParams.timeSeconds);
                    break;
                case TelemetryMetricTypes.Histogram:
                    m_Instance.SendHistogramMetric(metricParams.name, metricParams.timeSeconds);
                    break;
                case TelemetryMetricTypes.Sum:
                    m_Instance.SendSumMetric(metricParams.name, metricParams.timeSeconds);
                    break;
            }
        }
    }
}
