#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Uniject;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class MetricizedAppleStoreImpl : AppleStoreImpl
    {
        readonly ITelemetryMetricsService m_TelemetryMetricsService;

        [Preserve]
        internal MetricizedAppleStoreImpl(ICartValidator cartValidator,
            IAppleRetrieveProductsService retrieveProductsService,
            ITransactionLog transactionLog, IUtil util, ILogger logger, ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService)
            : base(cartValidator, retrieveProductsService, transactionLog, util, logger, telemetryDiagnostics)
        {
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        public override void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RetrieveProducts(products),
                TelemetryMetricDefinitions.retrieveProductsName);
        }

        public override void Purchase(ICart cart)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(cart), TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
