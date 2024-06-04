using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// This is C# representation of the Java Class BillingClient
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient">See more</a>
    /// </summary>
    class GoogleBillingClient : IGoogleBillingClient
    {
        const string k_AndroidProductClassName = "com.android.billingclient.api.QueryProductDetailsParams$Product";
        static AndroidJavaClass s_AndroidProductClassName;
        static AndroidJavaClass GetProductParamsClass()
        {
            s_AndroidProductClassName ??= new AndroidJavaClass(k_AndroidProductClassName);
            return s_AndroidProductClassName;
        }

        const string k_AndroidQueryProductDetailsParamsClassName = "com.android.billingclient.api.QueryProductDetailsParams";
        static AndroidJavaClass s_AndroidQueryProductDetailsParamsClassName;
        static AndroidJavaClass GetQueryProductDetailsParamsParamsClass()
        {
            s_AndroidQueryProductDetailsParamsClassName ??= new AndroidJavaClass(k_AndroidQueryProductDetailsParamsClassName);
            return s_AndroidQueryProductDetailsParamsClassName;
        }

        const string k_AndroidBillingFlowParamClassName = "com.android.billingclient.api.BillingFlowParams";
        static AndroidJavaClass s_BillingFlowParamsClass;
        static AndroidJavaClass GetBillingFlowParamClass()
        {
            s_BillingFlowParamsClass ??= new AndroidJavaClass(k_AndroidBillingFlowParamClassName);
            return s_BillingFlowParamsClass;
        }

        const string k_AndroidProductDetailsParamsClassName = "com.android.billingclient.api.BillingFlowParams$ProductDetailsParams";
        static AndroidJavaClass s_ProductDetailsParamsClass;
        static AndroidJavaClass GetProductDetailsParamsClass()
        {
            s_ProductDetailsParamsClass ??= new AndroidJavaClass(k_AndroidProductDetailsParamsClassName);
            return s_ProductDetailsParamsClass;
        }

        const string k_AndroidSubscriptionUpdateParamClassName = "com.android.billingclient.api.BillingFlowParams$SubscriptionUpdateParams";
        static AndroidJavaClass s_SubscriptionUpdateParamsClass;
        static AndroidJavaClass GetSubscriptionUpdateParamClass()
        {
            s_SubscriptionUpdateParamsClass ??= new AndroidJavaClass(k_AndroidSubscriptionUpdateParamClassName);
            return s_SubscriptionUpdateParamsClass;
        }

        const string k_AndroidConsumeParamsClassName = "com.android.billingclient.api.ConsumeParams";
        static AndroidJavaClass s_ConsumeParamsClass;
        static AndroidJavaClass GetConsumeParamsClass()
        {
            s_ConsumeParamsClass ??= new AndroidJavaClass(k_AndroidConsumeParamsClassName);
            return s_ConsumeParamsClass;
        }

        const string k_AndroidAcknowledgePurchaseParamsClassName = "com.android.billingclient.api.AcknowledgePurchaseParams";
        static AndroidJavaClass s_AcknowledgePurchaseParamsClass;
        static AndroidJavaClass GetAcknowledgePurchaseParamsClass()
        {
            s_AcknowledgePurchaseParamsClass ??= new AndroidJavaClass(k_AndroidAcknowledgePurchaseParamsClassName);
            return s_AcknowledgePurchaseParamsClass;
        }

        const string k_AndroidBillingClientClassName = "com.android.billingclient.api.BillingClient";
        static AndroidJavaClass s_BillingClientClass;
        static AndroidJavaClass GetBillingClientClass()
        {
            s_BillingClientClass ??= new AndroidJavaClass(k_AndroidBillingClientClassName);
            return s_BillingClientClass;
        }

        readonly AndroidJavaObject m_BillingClient;
        string m_ObfuscatedAccountId;
        string m_ObfuscatedProfileId;
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal GoogleBillingClient(IGooglePurchaseUpdatedListener googlePurchaseUpdatedListener, IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            using var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>("newBuilder", UnityActivity.GetCurrentActivity());
            builder.Call<AndroidJavaObject>("setListener", googlePurchaseUpdatedListener).Dispose();
            builder.Call<AndroidJavaObject>("enablePendingPurchases").Dispose();
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        public void SetObfuscationAccountId(string obfuscationAccountId)
        {
            m_ObfuscatedAccountId = obfuscationAccountId;
        }

        public void SetObfuscationProfileId(string obfuscationProfileId)
        {
            m_ObfuscatedProfileId = obfuscationProfileId;
        }

        public void StartConnection(IBillingClientStateListener billingClientStateListener)
        {
            m_BillingClient.Call("startConnection", billingClientStateListener);
        }

        public void EndConnection()
        {
            m_BillingClient.Call("endConnection");
        }

        public bool IsReady()
        {
            return m_BillingClient.Call<bool>("isReady");
        }

        public GoogleBillingConnectionState GetConnectionState()
        {
            return (GoogleBillingConnectionState)m_BillingClient.Call<int>("getConnectionState");
        }

        public void QueryPurchasesAsync(string skuType, Action<IGoogleBillingResult, IEnumerable<AndroidJavaObject>> onQueryPurchasesResponse)
        {
            var listener = new GooglePurchasesResponseListener(onQueryPurchasesResponse);
            m_BillingClient.Call("queryPurchasesAsync", skuType, listener);
        }

        public void QueryProductDetailsAsync(List<string> products, string type,
            Action<IGoogleBillingResult, List<AndroidJavaObject>> onProductDetailsResponseAction)
        {
            using var queryProductDetailsParams = QueryProductDetailsParams(products, type);
            var productDetailsResponseListener = new ProductDetailsResponseListener(onProductDetailsResponseAction, m_Util, m_TelemetryDiagnostics);
            m_BillingClient.Call("queryProductDetailsAsync", queryProductDetailsParams, productDetailsResponseListener);
        }

        static AndroidJavaObject QueryProductDetailsParams(List<string> products, string type)
        {
            using var queryProductDetailsParams = GetQueryProductDetailsParamsParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            using var queryProductDetailsParamsProductList = QueryProductDetailsParamsProductList(products, type);
            queryProductDetailsParams.Call<AndroidJavaObject>("setProductList", queryProductDetailsParamsProductList).Dispose();
            return queryProductDetailsParams.Call<AndroidJavaObject>("build");
        }

        static AndroidJavaObject QueryProductDetailsParamsProductList(List<string> products, string type)
        {
            var productJavaList = products.Select(product => QueryProductDetailsParamsProduct(type, product)).ToList();
            return productJavaList.ToJava();
        }

        static AndroidJavaObject QueryProductDetailsParamsProduct(string type, string product)
        {
            using var queryProductDetailsParamsProductBuilder = GetProductParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            queryProductDetailsParamsProductBuilder.Call<AndroidJavaObject>("setProductId", product).Dispose();
            queryProductDetailsParamsProductBuilder.Call<AndroidJavaObject>("setProductType", type).Dispose();
            var queryProductDetailsParamsProduct = queryProductDetailsParamsProductBuilder.Call<AndroidJavaObject>("build");
            return queryProductDetailsParamsProduct;
        }

        public AndroidJavaObject LaunchBillingFlow(AndroidJavaObject productDetails, string oldPurchaseToken, GooglePlayProrationMode? prorationMode)
        {
            // We currently only support 1 base plan so we can safely get the first one. Once we support multiple, this will need to be updated.
            using var subscriptionOfferDetails = productDetails.Call<AndroidJavaObject>("getSubscriptionOfferDetails");
            var firstSubscriptionOfferDetails = subscriptionOfferDetails?.Enumerate().ToList().FirstOrDefault();
            var offerToken = firstSubscriptionOfferDetails?.Call<string>("getOfferToken");

            using var productDetailsParamsBuilder = GetProductDetailsParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            productDetailsParamsBuilder.Call<AndroidJavaObject>("setProductDetails", productDetails).Dispose();
            if (offerToken != null)
            {
                productDetailsParamsBuilder.Call<AndroidJavaObject>("setOfferToken", offerToken).Dispose();
            }
            using var productDetailsParams = productDetailsParamsBuilder.Call<AndroidJavaObject>("build");
            var productDetailsParamsList = new List<AndroidJavaObject> { productDetailsParams }.ToJava();

            return m_BillingClient.Call<AndroidJavaObject>("launchBillingFlow", UnityActivity.GetCurrentActivity(), MakeBillingFlowParams(productDetailsParamsList, oldPurchaseToken, prorationMode));
        }

        AndroidJavaObject MakeBillingFlowParams(AndroidJavaObject productDetailsParamsList, string oldPurchaseToken, GooglePlayProrationMode? prorationMode)
        {
            var billingFlowParams = GetBillingFlowParamClass().CallStatic<AndroidJavaObject>("newBuilder");

            billingFlowParams = SetObfuscatedAccountIdIfNeeded(billingFlowParams);
            billingFlowParams = SetObfuscatedProfileIdIfNeeded(billingFlowParams);

            billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setProductDetailsParamsList", productDetailsParamsList);

            if (oldPurchaseToken != null && prorationMode != null)
            {
                var subscriptionUpdateParams = BuildSubscriptionUpdateParams(oldPurchaseToken, prorationMode.Value);
                billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setSubscriptionUpdateParams", subscriptionUpdateParams);
            }

            billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("build");
            return billingFlowParams;
        }

        static AndroidJavaObject BuildSubscriptionUpdateParams(string oldPurchaseToken, GooglePlayProrationMode prorationMode)
        {
            var subscriptionUpdateParams = GetSubscriptionUpdateParamClass().CallStatic<AndroidJavaObject>("newBuilder");

            subscriptionUpdateParams = subscriptionUpdateParams.Call<AndroidJavaObject>("setReplaceSkusProrationMode", (int)prorationMode);
            subscriptionUpdateParams = subscriptionUpdateParams.Call<AndroidJavaObject>("setOldSkuPurchaseToken", oldPurchaseToken);

            subscriptionUpdateParams = subscriptionUpdateParams.Call<AndroidJavaObject>("build");
            return subscriptionUpdateParams;
        }

        AndroidJavaObject SetObfuscatedProfileIdIfNeeded(AndroidJavaObject billingFlowParams)
        {
            if (m_ObfuscatedProfileId != null)
            {
                billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setObfuscatedProfileId", m_ObfuscatedProfileId);
            }

            return billingFlowParams;
        }

        AndroidJavaObject SetObfuscatedAccountIdIfNeeded(AndroidJavaObject billingFlowParams)
        {
            if (m_ObfuscatedAccountId != null)
            {
                billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setObfuscatedAccountId", m_ObfuscatedAccountId);
            }

            return billingFlowParams;
        }

        public void ConsumeAsync(string purchaseToken, Action<IGoogleBillingResult> onConsume)
        {
            using var consumeParamsBuilder = GetConsumeParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            consumeParamsBuilder.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken).Dispose();
            using var consumeParams = consumeParamsBuilder.Call<AndroidJavaObject>("build");

            m_BillingClient.Call("consumeAsync", consumeParams, new GoogleConsumeResponseListener(onConsume));
        }

        public void AcknowledgePurchase(string purchaseToken, Action<IGoogleBillingResult> onAcknowledge)
        {
            using var acknowledgePurchaseParamsBuilder = GetAcknowledgePurchaseParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            acknowledgePurchaseParamsBuilder.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken).Dispose();
            using var acknowledgePurchaseParams = acknowledgePurchaseParamsBuilder.Call<AndroidJavaObject>("build");

            m_BillingClient.Call("acknowledgePurchase", acknowledgePurchaseParams, new GoogleAcknowledgePurchaseListener(onAcknowledge));
        }
    }
}
