namespace UnityEngine.Purchasing.Telemetry
{
    struct TelemetryMetricParams
    {
        internal TelemetryMetricTypes type;
        internal string name;
        internal double timeSeconds;
        internal TelemetryMetricParams(TelemetryMetricTypes metricType, string metricName, double metricTimeSeconds)
        {
            type = metricType;
            name = metricName;
            timeSeconds = metricTimeSeconds;
        }
    }
}
