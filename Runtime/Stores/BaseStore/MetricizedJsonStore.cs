using System.Collections.Generic;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class MetricizedJsonStore : JsonStore
    {
        readonly ITelemetryMetricsService m_TelemetryMetricsService;

        internal MetricizedJsonStore(ICartValidator cartValidator, ILogger logger, string storeName,
            ITelemetryMetricsService telemetryMetricsService) : base(cartValidator, logger, storeName)
        {
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.FetchProducts(products),
                TelemetryMetricDefinitions.fetchProductsName);
        }

        public override void Purchase(ICart cart)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(cart),
                TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
