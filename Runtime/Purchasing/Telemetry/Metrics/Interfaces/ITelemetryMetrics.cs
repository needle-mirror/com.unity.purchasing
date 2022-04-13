namespace UnityEngine.Purchasing.Telemetry
{
    interface ITelemetryMetrics
    {
        ITelemetryMetricEvent CreateAndStartMetricEvent(TelemetryMetricTypes metricType, string metricName);
    }
}
