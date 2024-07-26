#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreService : IGooglePlayStoreService
    {
        readonly IQueryProductDetailsService m_QueryProductDetailsService;
        readonly IGoogleLastKnownProductService m_GoogleLastKnownProductService;
        readonly IGooglePurchaseService m_GooglePurchaseService;
        readonly IGoogleFinishTransactionUseCase m_GoogleFinishTransactionUseCase;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;
        readonly IGoogleQueryPurchasesUseCase m_GoogleQueryPurchasesUseCase;
        readonly IGooglePlayCheckEntitlementUseCase m_GoogleCheckEntitlementUseCase;
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGooglePlayStoreConnectionService m_GooglePlayStoreConnectionService;

        internal GooglePlayStoreService(
            IGoogleBillingClient billingClient,
            IGooglePlayStoreConnectionService connectionService,
            IQueryProductDetailsService queryProductDetailsService,
            IGoogleLastKnownProductService lastKnownProductService,
            IGooglePurchaseService purchaseService,
            IGoogleFinishTransactionUseCase finishTransactionUseCase,
            IGoogleQueryPurchasesUseCase queryPurchasesUseCase,
            IGooglePlayCheckEntitlementUseCase googleCheckEntitlementUseCase,
            ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_QueryProductDetailsService = queryProductDetailsService;
            m_GoogleLastKnownProductService = lastKnownProductService;
            m_GooglePurchaseService = purchaseService;
            m_GoogleFinishTransactionUseCase = finishTransactionUseCase;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            m_GoogleQueryPurchasesUseCase = queryPurchasesUseCase;
            m_GoogleCheckEntitlementUseCase = googleCheckEntitlementUseCase;
            m_BillingClient = billingClient;
            m_GooglePlayStoreConnectionService = connectionService;
        }

        public virtual async void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductException> onRetrieveProductsFailed)
        {
            var productDescriptions = await m_QueryProductDetailsService.QueryProductDescriptions(products);
            onProductsReceived(productDescriptions);
        }

        public void Purchase(ProductDefinition product)
        {
            Purchase(product, null, null);
        }

        public virtual void Purchase(ProductDefinition product, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            m_GoogleLastKnownProductService.LastKnownOldProductId = oldProduct?.definition.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownProductId = product.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownProrationMode = desiredProrationMode;
            m_GooglePurchaseService.Purchase(product, oldProduct, desiredProrationMode);
        }

        public void FinishTransaction(ProductDefinition? product, string? purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            m_GoogleFinishTransactionUseCase.FinishTransaction(product, purchaseToken, onTransactionFinished);
        }

        public async void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed)
        {
            if (onQueryPurchaseSucceed == null)
            {
                throw new ArgumentException("FetchPurchases was called with a null callback, the request will not be executed.");
            }

            try
            {
                await TryFetchPurchases(onQueryPurchaseSucceed);
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.FetchPurchasesError, ex);
            }
        }

        protected virtual async Task TryFetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed)
        {
            var purchases = await m_GoogleQueryPurchasesUseCase.QueryPurchases();
            onQueryPurchaseSucceed(purchases);
        }

        public void CheckEntitlement(ProductDefinition product, Action<ProductDefinition, EntitlementStatus> onEntitlementChecked)
        {
            m_GoogleCheckEntitlementUseCase.CheckEntitlement(product, onEntitlementChecked);
        }

        public void SetObfuscatedAccountId(string obfuscatedAccountId)
        {
            m_BillingClient.SetObfuscationAccountId(obfuscatedAccountId);
        }

        public void SetObfuscatedProfileId(string obfuscatedProfileId)
        {
            m_BillingClient.SetObfuscationProfileId(obfuscatedProfileId);
        }

        public virtual bool IsConnectionReady()
        {
            return m_GooglePlayStoreConnectionService.IsReady();
        }
    }
}
