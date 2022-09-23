using System;
using System.Diagnostics;

namespace UnityEngine.Purchasing.Telemetry
{
    class TelemetryMetricEvent : ITelemetryMetricEvent
    {
        readonly ITelemetryMetricsInstanceWrapper m_TelemetryMetricsInstanceWrapper;
        readonly TelemetryMetricTypes m_MetricType;
        readonly string m_MetricName;
        Stopwatch m_Stopwatch = new Stopwatch();

        internal TelemetryMetricEvent(ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper, TelemetryMetricTypes metricType, string metricName)
        {
            m_TelemetryMetricsInstanceWrapper = telemetryMetricsInstanceWrapper;
            m_MetricType = metricType;
            m_MetricName = metricName;
        }

        public void StartMetric()
        {
            if (m_Stopwatch != null)
            {
                if (!m_Stopwatch.IsRunning)
                {
                    m_Stopwatch.Start();
                }
                else
                {
                    throw new Exception("Metric was already started.");
                }
            }
            else
            {
                throw new Exception("Metric was already sent.");
            }
        }

        public void StopAndSendMetric()
        {
            if (m_Stopwatch != null)
            {
                m_TelemetryMetricsInstanceWrapper?.SendMetric(m_MetricType, m_MetricName, m_Stopwatch.Elapsed.Seconds);
                m_Stopwatch = null;
            }
            else
            {
                throw new Exception("Metric was already sent.");
            }
        }
    }
}
