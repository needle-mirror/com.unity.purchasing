using System;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryMetrics : ITelemetryMetrics
    {
        ITelemetryMetricsInstanceWrapper m_TelemetryMetricsInstanceWrapper;

        public TelemetryMetrics(ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper)
        {
            m_TelemetryMetricsInstanceWrapper = telemetryMetricsInstanceWrapper;
        }

        public ITelemetryMetricEvent CreateAndStartMetricEvent(TelemetryMetricTypes metricType, string metricName)
        {
            ITelemetryMetricEvent metricEvent = new TelemetryMetricEvent(m_TelemetryMetricsInstanceWrapper, metricType, metricName);
            metricEvent.StartMetric();
            return metricEvent;
        }
    }
}
