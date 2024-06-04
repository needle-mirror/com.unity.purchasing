#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Stores.Util;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class MetricizedGooglePlayStoreService : GooglePlayStoreService
    {
        readonly ITelemetryMetricsService m_TelemetryMetricsService;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal MetricizedGooglePlayStoreService(
            IGoogleBillingClient billingClient,
            IQueryProductDetailsService queryProductDetailsService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionService finishTransactionService,
            IGoogleQueryPurchasesService queryPurchasesService,
            IBillingClientStateListener billingClientStateListener,
            IGoogleLastKnownProductService lastKnownProductService,
            ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService,
            ILogger logger,
            IRetryPolicy retryPolicy,
            IUtil util)
            : base(billingClient, queryProductDetailsService, purchaseService, finishTransactionService,
                queryPurchasesService, billingClientStateListener, lastKnownProductService,
                telemetryDiagnostics, logger, retryPolicy, util)
        {
            m_TelemetryDiagnostics = telemetryDiagnostics;
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        protected override void DequeueQueryProducts(GoogleBillingResponseCode googleBillingResponseCode)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.DequeueQueryProducts(googleBillingResponseCode),
                TelemetryMetricDefinitions.dequeueQueryProductsTimeName);
        }

        protected override void DequeueFetchPurchases()
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                base.DequeueFetchPurchases,
                TelemetryMetricDefinitions.dequeueQueryPurchasesTimeName);
        }

        public override void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products,
            Action<List<ProductDescription>, IGoogleBillingResult> onProductsReceived,
            Action<GoogleRetrieveProductsFailureReason, GoogleBillingResponseCode> onRetrieveProductsFailed)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RetrieveProducts(products, onProductsReceived, onRetrieveProductsFailed),
                TelemetryMetricDefinitions.retrieveProductsName);
        }

        public override void Purchase(ProductDefinition product, Product? oldProduct,
            GooglePlayProrationMode? desiredProrationMode)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(product, oldProduct, desiredProrationMode),
                TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
