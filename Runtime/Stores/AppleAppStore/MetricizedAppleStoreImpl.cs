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
            IAppleFetchProductsService fetchProductsService,
            ITransactionLog transactionLog, IUtil util, ILogger logger, ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService)
            : base(cartValidator, fetchProductsService, transactionLog, util, logger, telemetryDiagnostics)
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
                () => base.Purchase(cart), TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
