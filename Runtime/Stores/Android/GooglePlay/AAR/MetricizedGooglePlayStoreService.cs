#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class MetricizedGooglePlayStoreService : GooglePlayStoreService
    {
        readonly ITelemetryMetricsService m_TelemetryMetricsService;

        [Preserve]
        internal MetricizedGooglePlayStoreService(IGoogleBillingClient billingClient,
            IGooglePlayStoreConnectionService connectionService,
            IQueryProductDetailsService queryProductDetailsService,
            IGoogleLastKnownProductService lastKnownProductService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionUseCase finishTransactionUseCase,
            IGoogleQueryPurchasesUseCase queryPurchasesUseCase,
            IGooglePlayCheckEntitlementUseCase googleCheckEntitlementUseCase,
            ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService)
            : base(billingClient,
                connectionService,
                queryProductDetailsService,
                lastKnownProductService,
                purchaseService,
                finishTransactionUseCase,
                queryPurchasesUseCase,
                googleCheckEntitlementUseCase,
                telemetryDiagnostics)
        {
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products,
            Action<List<ProductDescription>> onProductsReceived,
            Action<GoogleFetchProductException> onFetchProductsFailed)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.FetchProducts(products, onProductsReceived, onFetchProductsFailed),
                TelemetryMetricDefinitions.fetchProductsName);
        }

        public override void Purchase(ProductDefinition product, Order? currentOrder,
            GooglePlayReplacementMode? desiredReplacementMode)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(product, currentOrder, desiredReplacementMode),
                TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
