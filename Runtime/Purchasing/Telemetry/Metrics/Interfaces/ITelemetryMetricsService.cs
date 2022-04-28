using System;

namespace UnityEngine.Purchasing.Telemetry
{
    interface ITelemetryMetricsService
    {
        void ExecuteTimedAction(Action timedAction, TelemetryMetricDefinition metricDefinition);
        ITelemetryMetricEvent CreateAndStartMetricEvent(TelemetryMetricDefinition metricDefinition);
    }
}
