using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class MetricizedJsonStore : JSONStore
    {
        ITelemetryMetricsService m_TelemetryMetricsService;

        public MetricizedJsonStore(ITelemetryMetricsService telemetryMetricsService)
        {
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        public override void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RetrieveProducts(products),
                TelemetryMetricDefinitions.retrieveProductsName);
        }

        public override void Purchase(ProductDefinition product, string developerPayload)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(product, developerPayload),
                TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
