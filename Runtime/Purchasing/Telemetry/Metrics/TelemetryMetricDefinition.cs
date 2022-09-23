namespace UnityEngine.Purchasing.Telemetry
{
    struct TelemetryMetricDefinition
    {
        public TelemetryMetricTypes MetricType { get; }
        public string MetricName { get; }

        public TelemetryMetricDefinition(string metricName,
            TelemetryMetricTypes metricType = TelemetryMetricTypes.Histogram)
        {
            MetricName = metricName;
            MetricType = metricType;
        }

        public static implicit operator TelemetryMetricDefinition(string name)
        {
            return new TelemetryMetricDefinition(name);
        }
    }
}
