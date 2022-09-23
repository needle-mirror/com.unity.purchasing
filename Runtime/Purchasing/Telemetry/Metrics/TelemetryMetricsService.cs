using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryMetricsService : ITelemetryMetricsService
    {
        readonly ITelemetryMetricsInstanceWrapper m_TelemetryMetricsInstanceWrapper;

        public TelemetryMetricsService(ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper)
        {
            m_TelemetryMetricsInstanceWrapper = telemetryMetricsInstanceWrapper;
        }

        public void ExecuteTimedAction(Action timedAction, TelemetryMetricDefinition metricDefinition)
        {
            var handle = CreateAndStartMetricEvent(metricDefinition);
            timedAction();
            handle.StopAndSendMetric();
        }

        public ITelemetryMetricEvent CreateAndStartMetricEvent(TelemetryMetricDefinition metricDefinition)
        {
            ITelemetryMetricEvent metricEvent = new TelemetryMetricEvent(m_TelemetryMetricsInstanceWrapper, metricDefinition.MetricType, metricDefinition.MetricName);
            metricEvent.StartMetric();
            return metricEvent;
        }
    }
}
