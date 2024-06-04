#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Stores.Util;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class QueryProductDetailsService : IQueryProductDetailsService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGoogleCachedQueryProductDetailsService m_GoogleCachedQueryProductDetailsService;
        readonly IProductDetailsConverter m_ProductDetailsConverter;
        readonly IRetryPolicy m_RetryPolicy;
        readonly IGoogleProductCallback m_GoogleProductCallback;
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal QueryProductDetailsService(IGoogleBillingClient billingClient, IGoogleCachedQueryProductDetailsService googleCachedQueryProductDetailsService,
            IProductDetailsConverter productDetailsConverter, IRetryPolicy retryPolicy, IGoogleProductCallback googleProductCallback, IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_BillingClient = billingClient;
            m_GoogleCachedQueryProductDetailsService = googleCachedQueryProductDetailsService;
            m_ProductDetailsConverter = productDetailsConverter;
            m_RetryPolicy = retryPolicy;
            m_GoogleProductCallback = googleProductCallback;
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }


        public void QueryAsyncProduct(ProductDefinition product, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse)
        {
            QueryAsyncProduct(new List<ProductDefinition>
            {
                product
            }.AsReadOnly(), onProductDetailsResponse);
        }

        public void QueryAsyncProduct(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>, IGoogleBillingResult> onProductDetailsResponse)
        {
            QueryAsyncProduct(products,
                (productDetails, responseCode) => onProductDetailsResponse(m_ProductDetailsConverter.ConvertOnQueryProductDetailsResponse(productDetails), responseCode));
        }

        public void QueryAsyncProduct(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse)
        {
            var retryCount = 0;

            m_RetryPolicy.Invoke(retryAction => QueryAsyncProductWithRetries(products, onProductDetailsResponse, retryAction), OnActionRetry);

            void OnActionRetry()
            {
                m_GoogleProductCallback.NotifyQueryProductDetailsFailed(++retryCount);
            }
        }

        void QueryAsyncProductWithRetries(IReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse, Action retryQuery)
        {
            try
            {
                TryQueryAsyncProductWithRetries(products, onProductDetailsResponse, retryQuery);
            }
            catch (Exception ex)
            {
                m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.QueryAsyncSkuError, ex);
                Debug.LogError($"Unity IAP - QueryAsyncProductWithRetries: {ex}");
            }
        }

        void TryQueryAsyncProductWithRetries(IReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse, Action retryQuery)
        {
            var consolidator = new ProductDetailsResponseConsolidator(m_Util, m_TelemetryDiagnostics, productDetailsQueryResponse =>
            {
                m_GoogleCachedQueryProductDetailsService.AddCachedQueriedProductDetails(productDetailsQueryResponse.ProductDetails());
                if (ShouldRetryQuery(products, productDetailsQueryResponse))
                {
                    retryQuery();
                }
                else
                {
                    onProductDetailsResponse(GetCachedProductDetails(products).ToList(), productDetailsQueryResponse.GetGoogleBillingResult());
                }
            });
            QueryInAppsAsync(products, consolidator);
            QuerySubsAsync(products, consolidator);
        }

        bool ShouldRetryQuery(IEnumerable<ProductDefinition> requestedProducts, IProductDetailsQueryResponse queryResponse)
        {
            return !AreAllProductDetailsCached(requestedProducts) && queryResponse.IsRecoverable();
        }

        bool AreAllProductDetailsCached(IEnumerable<ProductDefinition> products)
        {
            return products.Select(m_GoogleCachedQueryProductDetailsService.Contains).All(isCached => isCached);
        }

        IEnumerable<AndroidJavaObject> GetCachedProductDetails(IEnumerable<ProductDefinition> products)
        {
            var cachedProducts = products.Where(m_GoogleCachedQueryProductDetailsService.Contains).ToList();
            return m_GoogleCachedQueryProductDetailsService.GetCachedQueriedProductDetails(cachedProducts);
        }

        void QueryInAppsAsync(IEnumerable<ProductDefinition> products, IProductDetailsResponseConsolidator consolidator)
        {
            var productList = products
                .Where(product => product.type != ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QueryProductDetails(productList, GoogleProductTypeEnum.InApp(), consolidator);
        }

        void QuerySubsAsync(IEnumerable<ProductDefinition> products, IProductDetailsResponseConsolidator consolidator)
        {
            var productList = products
                .Where(product => product.type == ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QueryProductDetails(productList, GoogleProductTypeEnum.Sub(), consolidator);
        }

        void QueryProductDetails(List<string> productList, string type, IProductDetailsResponseConsolidator consolidator)
        {
            if (productList.Count == 0)
            {
                consolidator.Consolidate(new GoogleBillingResult(null), new List<AndroidJavaObject>());
                return;
            }

            m_BillingClient.QueryProductDetailsAsync(productList, type, consolidator.Consolidate);
        }
    }
}
