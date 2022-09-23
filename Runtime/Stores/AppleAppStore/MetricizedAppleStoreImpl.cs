using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uniject;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class MetricizedAppleStoreImpl : AppleStoreImpl
    {
        readonly ITelemetryMetricsService m_TelemetryMetricsService;

        public MetricizedAppleStoreImpl(IUtil util, ITelemetryDiagnostics telemetryDiagnostics,
            ITelemetryMetricsService telemetryMetricsService) : base(util, telemetryDiagnostics)
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

        public override void RestoreTransactions(Action<bool> callback)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RestoreTransactions(callback), TelemetryMetricDefinitions.restoreTransactionName);
        }

        public override void RefreshAppReceipt(Action<string> successCallback, Action errorCallback)
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

        public override void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.RetrieveProducts(products),
                TelemetryMetricDefinitions.retrieveProductsName);
        }

        public override void Purchase(ProductDefinition product, string developerPayload)
        {
            m_TelemetryMetricsService.ExecuteTimedAction(
                () => base.Purchase(product, developerPayload), TelemetryMetricDefinitions.initPurchaseName);
        }
    }
}
