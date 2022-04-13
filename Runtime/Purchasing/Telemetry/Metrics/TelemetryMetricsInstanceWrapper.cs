using System;
using Unity.Services.Core.Telemetry.Internal;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryMetricsInstanceWrapper : ITelemetryMetricsInstanceWrapper
    {
        IMetrics m_Instance;
        TelemetryQueue<TelemetryMetricParams> m_Queue;

        public TelemetryMetricsInstanceWrapper()
        {
            m_Queue = new TelemetryQueue<TelemetryMetricParams>(SendMetricByType);
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
                SendMetricByType(telemetryMetricParams);
            }
            else
            {
                m_Queue.QueueEvent(telemetryMetricParams);
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
