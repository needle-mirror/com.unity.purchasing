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
            IQuerySkuDetailsService querySkuDetailsService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionService finishTransactionService,
            IGoogleQueryPurchasesService queryPurchasesService,
            IBillingClientStateListener billingClientStateListener,
            IGooglePriceChangeService priceChangeService,
            IGoogleLastKnownProductService lastKnownProductService,
            ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService,
            ILogger logger,
            IRetryPolicy retryPolicy,
            IUtil util)
            : base(billingClient, querySkuDetailsService, purchaseService, finishTransactionService,
                queryPurchasesService, billingClientStateListener, priceChangeService, lastKnownProductService,
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
            Action<List<ProductDescription>> onProductsReceived,
            Action<GoogleRetrieveProductsFailureReason, GoogleBillingResponseCode> onRetrieveProductsFailed)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RetrieveProducts(products, onProductsReceived, onRetrieveProductsFailed),
                TelemetryMetricDefinitions.retrieveProductsName);
        }

        public override void Purchase(ProductDefinition product, Product oldProduct,
            GooglePlayProrationMode? desiredProrationMode)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(product, oldProduct, desiredProrationMode),
                TelemetryMetricDefinitions.initPurchaseName);
        }

        public override void ConfirmSubscriptionPriceChange(ProductDefinition product,
            Action<IGoogleBillingResult> onPriceChangeAction)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.ConfirmSubscriptionPriceChange(product, onPriceChangeAction),
                TelemetryMetricDefinitions.confirmSubscriptionPriceChangeName);
        }
    }
}
