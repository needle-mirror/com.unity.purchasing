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
            IAppleRetrieveProductsService retrieveProductsService, IAppleReceiptConverter receiptConverter,
            ITransactionLog transactionLog, IUtil util, ILogger logger, ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService)
            : base(cartValidator, retrieveProductsService, receiptConverter, transactionLog, util, logger, telemetryDiagnostics)
        {
            m_TelemetryMetricsService = telemetryMetricsService;
        }

        public override void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action errorCallback)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.FetchStorePromotionOrder(successCallback, errorCallback),
                TelemetryMetricDefinitions.fetchStorePromotionOrderName);
        }

        public override void FetchStorePromotionVisibility(Product product,
            Action<string, AppleStorePromotionVisibility> successCallback, Action errorCallback)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.FetchStorePromotionVisibility(product, successCallback, errorCallback),
                TelemetryMetricDefinitions.fetchStorePromotionVisibilityName);
        }

        public override void SetStorePromotionOrder(List<Product> products)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.SetStorePromotionOrder(products), TelemetryMetricDefinitions.setStorePromotionOrderName);
        }

        public override void RestoreTransactions(Action<bool, string?>? callback)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RestoreTransactions(callback), TelemetryMetricDefinitions.restoreTransactionName);
        }

        public override void RefreshAppReceipt(Action<string> successCallback, Action<string> errorCallback)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RefreshAppReceipt(successCallback, errorCallback),
                TelemetryMetricDefinitions.refreshAppReceiptName);
        }

        public override void ContinuePromotionalPurchases()
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                base.ContinuePromotionalPurchases, TelemetryMetricDefinitions.continuePromotionalPurchasesName);
        }

        public override void PresentCodeRedemptionSheet()
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                base.PresentCodeRedemptionSheet, TelemetryMetricDefinitions.presentCodeRedemptionSheetName);
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
