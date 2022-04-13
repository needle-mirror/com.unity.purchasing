namespace UnityEngine.Purchasing.Telemetry
{
    interface ITelemetryMetricEvent
    {
        void StartMetric();
        void StopAndSendMetric();
    }
}
