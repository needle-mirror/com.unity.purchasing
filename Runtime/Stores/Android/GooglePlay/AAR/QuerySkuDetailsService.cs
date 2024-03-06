#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Stores.Util;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class QuerySkuDetailsService : IQuerySkuDetailsService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        readonly ISkuDetailsConverter m_SkuDetailsConverter;
        readonly IRetryPolicy m_RetryPolicy;
        readonly IGoogleProductCallback m_GoogleProductCallback;
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal QuerySkuDetailsService(IGoogleBillingClient billingClient, IGoogleCachedQuerySkuDetailsService googleCachedQuerySkuDetailsService,
            ISkuDetailsConverter skuDetailsConverter, IRetryPolicy retryPolicy, IGoogleProductCallback googleProductCallback, IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_BillingClient = billingClient;
            m_GoogleCachedQuerySkuDetailsService = googleCachedQuerySkuDetailsService;
            m_SkuDetailsConverter = skuDetailsConverter;
            m_RetryPolicy = retryPolicy;
            m_GoogleProductCallback = googleProductCallback;
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }


        public void QueryAsyncSku(ProductDefinition product, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            QueryAsyncSku(new List<ProductDefinition>
            {
                product
            }.AsReadOnly(), onSkuDetailsResponse);
        }

        public void QueryAsyncSku(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onSkuDetailsResponse)
        {
            QueryAsyncSku(products,
                skus => onSkuDetailsResponse(m_SkuDetailsConverter.ConvertOnQuerySkuDetailsResponse(skus)));
        }

        public void QueryAsyncSku(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            var retryCount = 0;

            m_RetryPolicy.Invoke(retryAction => QueryAsyncSkuWithRetries(products, onSkuDetailsResponse, retryAction), OnActionRetry);

            void OnActionRetry()
            {
                m_GoogleProductCallback.NotifyQueryProductDetailsFailed(++retryCount);
            }
        }

        void QueryAsyncSkuWithRetries(IReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse, Action retryQuery)
        {
            try
            {
                TryQueryAsyncSkuWithRetries(products, onSkuDetailsResponse, retryQuery);
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.QueryAsyncSkuError, ex);
                Debug.LogError($"Unity IAP - QueryAsyncSkuWithRetries: {ex}");
            }
        }

        void TryQueryAsyncSkuWithRetries(IReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse, Action retryQuery)
        {
            var consolidator = new SkuDetailsResponseConsolidator(m_Util, m_TelemetryDiagnostics, skuDetailsQueryResponse =>
            {
                m_GoogleCachedQuerySkuDetailsService.AddCachedQueriedSkus(skuDetailsQueryResponse.SkuDetails());
                if (ShouldRetryQuery(products, skuDetailsQueryResponse))
                {
                    retryQuery();
                }
                else
                {
                    onSkuDetailsResponse(GetCachedSkuDetails(products).ToList());
                }
            });
            QueryInAppsAsync(products, consolidator);
            QuerySubsAsync(products, consolidator);
        }

        bool ShouldRetryQuery(IEnumerable<ProductDefinition> requestedProducts, ISkuDetailsQueryResponse queryResponse)
        {
            return !AreAllSkuDetailsCached(requestedProducts) && queryResponse.IsRecoverable();
        }

        bool AreAllSkuDetailsCached(IEnumerable<ProductDefinition> products)
        {
            return products.Select(m_GoogleCachedQuerySkuDetailsService.Contains).All(isCached => isCached);
        }

        IEnumerable<AndroidJavaObject> GetCachedSkuDetails(IEnumerable<ProductDefinition> products)
        {
            var cachedProducts = products.Where(m_GoogleCachedQuerySkuDetailsService.Contains).ToList();
            return m_GoogleCachedQuerySkuDetailsService.GetCachedQueriedSkus(cachedProducts);
        }

        void QueryInAppsAsync(IEnumerable<ProductDefinition> products, ISkuDetailsResponseConsolidator consolidator)
        {
            var skus = products
                .Where(product => product.type != ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QuerySkuDetails(skus, GoogleSkuTypeEnum.InApp(), consolidator);
        }

        void QuerySubsAsync(IEnumerable<ProductDefinition> products, ISkuDetailsResponseConsolidator consolidator)
        {
            var skus = products
                .Where(product => product.type == ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QuerySkuDetails(skus, GoogleSkuTypeEnum.Sub(), consolidator);
        }

        void QuerySkuDetails(List<string> skus, string type, ISkuDetailsResponseConsolidator consolidator)
        {
            m_BillingClient.QuerySkuDetailsAsync(skus, type, consolidator.Consolidate);
        }
    }
}
