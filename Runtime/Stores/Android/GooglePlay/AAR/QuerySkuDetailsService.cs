using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing
{
    class QuerySkuDetailsService : IQuerySkuDetailsService
    {
        const string k_AndroidSkuDetailsParamClassName = "com.android.billingclient.api.SkuDetailsParams";
        const string k_InApp = "inapp";
        const string k_Subs = "subs";

        IGoogleBillingClient m_BillingClient;
        IGoogleCachedQuerySkuDetailsService m_GoogleCachedQuerySkuDetailsService;
        const int k_RequiredNumberOfCallbacks = 2;
        int m_NumberReceivedCallbacks;
        List<AndroidJavaObject> m_QueriedSkuDetails = new List<AndroidJavaObject>();
        IUtil m_Util;

        static AndroidJavaClass GetSkuDetailsParamClass()
        {
            return new AndroidJavaClass(k_AndroidSkuDetailsParamClassName);
        }

        internal QuerySkuDetailsService(IGoogleBillingClient billingClient, IGoogleCachedQuerySkuDetailsService googleCachedQuerySkuDetailsService, IUtil util)
        {
            m_BillingClient = billingClient;
            m_GoogleCachedQuerySkuDetailsService = googleCachedQuerySkuDetailsService;
            m_Util = util;
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
                skus => SkuDetailsConverter.ConvertOnQuerySkuDetailsResponse(skus, onSkuDetailsResponse));
        }

        public void QueryAsyncSku(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            QueryInAppsAsync(products, onSkuDetailsResponse);
            QuerySubsAsync(products, onSkuDetailsResponse);
        }

        void QueryInAppsAsync(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            var skus = products
                .Where(product => product.type != ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QuerySkuDetails(skus, k_InApp, onSkuDetailsResponse);
        }

        void QuerySubsAsync(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            var skus = products
                .Where(product => product.type == ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            QuerySkuDetails(skus, k_Subs, onSkuDetailsResponse);
        }

        void QuerySkuDetails(List<string> skus, string type, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            var skuDetailsParamsBuilder = GetSkuDetailsParamClass().CallStatic<AndroidJavaObject>("newBuilder");
            skuDetailsParamsBuilder = skuDetailsParamsBuilder.Call<AndroidJavaObject>("setSkusList", skus.ToJava());
            skuDetailsParamsBuilder = skuDetailsParamsBuilder.Call<AndroidJavaObject>("setType", type);

            var listener = new SkuDetailsResponseListener((billingResult, skuDetails) => ConsolidateOnSkuDetailsReceived(billingResult, skuDetails, onSkuDetailsResponse), m_Util);

            m_BillingClient.QuerySkuDetailsAsync(skuDetailsParamsBuilder, listener);
        }

        void ConsolidateOnSkuDetailsReceived(AndroidJavaObject javaBillingResult, AndroidJavaObject skuDetails, Action<List<AndroidJavaObject>> onSkuDetailsResponse)
        {
            m_NumberReceivedCallbacks++;

            var billingResult = new GoogleBillingResult(javaBillingResult);
            if (billingResult.responseCode == BillingClientResponseEnum.OK())
            {
                AddToQueriedSkuDetails(skuDetails);
            }

            if (m_NumberReceivedCallbacks >= k_RequiredNumberOfCallbacks)
            {
                m_GoogleCachedQuerySkuDetailsService.AddCachedQueriedSkus(m_QueriedSkuDetails);
                onSkuDetailsResponse(m_QueriedSkuDetails);
                Clear();
            }
        }

        void AddToQueriedSkuDetails(AndroidJavaObject skusDetails)
        {
            var size = skusDetails.Call<int>("size");
            for (var index = 0; index < size; index++)
            {
                m_QueriedSkuDetails.Add(skusDetails.Call<AndroidJavaObject>("get", index));
            }
        }

        void Clear()
        {
            m_NumberReceivedCallbacks = 0;
            m_QueriedSkuDetails = new List<AndroidJavaObject>();
        }
    }
}
