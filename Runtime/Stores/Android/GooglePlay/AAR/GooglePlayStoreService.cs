#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual async void FetchProducts(IReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleFetchProductException> onFetchProductsFailed)
        {
            try
            {
                var productDescriptions = await m_QueryProductDetailsService.QueryProductDescriptions(products);
                onProductsReceived(productDescriptions);
            }
            catch (GoogleFetchProductException e)
            {
                onFetchProductsFailed(e);
            }
            catch (Exception ex)
            {
                var description = new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown,
                    ex.Message, false);
                var googleFetchProductException = new GoogleFetchProductException(GoogleFetchProductsFailureReason.Unknown,
                    GoogleBillingResponseCode.FatalError, description);

                onFetchProductsFailed(googleFetchProductException);
            }
        }

        public void Purchase(ProductDefinition product)
        {
            Purchase(product, null, null);
        }

        public virtual void Purchase(ProductDefinition product, Order? currentOrder, GooglePlayReplacementMode? desiredReplacementMode)
        {
            m_GoogleLastKnownProductService.LastKnownOldProductId = currentOrder?.CartOrdered.Items().FirstOrDefault()?.Product.definition.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownProductId = product.storeSpecificId;
            m_GoogleLastKnownProductService.LastKnownReplacementMode = desiredReplacementMode;
            m_GooglePurchaseService.Purchase(product, currentOrder, desiredReplacementMode);
        }

        public async Task FinishTransaction(ProductDefinition? product, string? purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            await m_GoogleFinishTransactionUseCase.FinishTransaction(product, purchaseToken, onTransactionFinished);
        }

        public async void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed, Action<string?> onQueryPurchaseFailed)
        {
            try
            {
                await TryFetchPurchases(onQueryPurchaseSucceed);
            }
            catch (Exception ex)
            {
                onQueryPurchaseFailed.Invoke(ex.Message);
            }
        }

        async Task TryFetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed)
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
