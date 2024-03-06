using System;
using System.Collections.Generic;
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
        const string k_AndroidSkuDetailsParamClassName = "com.android.billingclient.api.SkuDetailsParams";
        static AndroidJavaClass s_SkuDetailsParamsClass;
        static AndroidJavaClass GetSkuDetailsParamClass()
        {
            s_SkuDetailsParamsClass ??= new AndroidJavaClass(k_AndroidSkuDetailsParamClassName);
            return s_SkuDetailsParamsClass;
        }

        const string k_AndroidBillingFlowParamClassName = "com.android.billingclient.api.BillingFlowParams";
        static AndroidJavaClass s_BillingFlowParamsClass;
        static AndroidJavaClass GetBillingFlowParamClass()
        {
            s_BillingFlowParamsClass ??= new AndroidJavaClass(k_AndroidBillingFlowParamClassName);
            return s_BillingFlowParamsClass;
        }

        const string k_AndroidSubscriptionUpdateParamClassName = "com.android.billingclient.api.BillingFlowParams$SubscriptionUpdateParams";
        static AndroidJavaClass s_SubscriptionUpdateParamsClass;
        static AndroidJavaClass GetSubscriptionUpdateParamClass()
        {
            s_SubscriptionUpdateParamsClass ??= new AndroidJavaClass(k_AndroidSubscriptionUpdateParamClassName);
            return s_SubscriptionUpdateParamsClass;
        }

        const string k_AndroidPriceChangeFlowParamClassName = "com.android.billingclient.api.PriceChangeFlowParams";
        static AndroidJavaClass s_PriceChangeFlowParamsClass;
        static AndroidJavaClass GetPriceChangeFlowParamClass()
        {
            s_PriceChangeFlowParamsClass ??= new AndroidJavaClass(k_AndroidPriceChangeFlowParamClassName);
            return s_PriceChangeFlowParamsClass;
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

        public void QuerySkuDetailsAsync(List<string> skus, string type,
            Action<IGoogleBillingResult, List<AndroidJavaObject>> onSkuDetailsResponseAction)
        {
            using var skusJavaList = skus.ToJava();
            using var skuDetailsParamsBuilder = GetSkuDetailsParamClass().CallStatic<AndroidJavaObject>("newBuilder");
            skuDetailsParamsBuilder.Call<AndroidJavaObject>("setSkusList", skusJavaList).Dispose();
            skuDetailsParamsBuilder.Call<AndroidJavaObject>("setType", type).Dispose();
            using var skuDetailsParams = skuDetailsParamsBuilder.Call<AndroidJavaObject>("build");

            var listener = new SkuDetailsResponseListener(onSkuDetailsResponseAction, m_Util, m_TelemetryDiagnostics);
            m_BillingClient.Call("querySkuDetailsAsync", skuDetailsParams, listener);
        }

        public AndroidJavaObject LaunchBillingFlow(AndroidJavaObject sku, string oldPurchaseToken, GooglePlayProrationMode? prorationMode)
        {
            return m_BillingClient.Call<AndroidJavaObject>("launchBillingFlow", UnityActivity.GetCurrentActivity(), MakeBillingFlowParams(sku, oldPurchaseToken, prorationMode));
        }

        AndroidJavaObject MakeBillingFlowParams(AndroidJavaObject sku, string oldPurchaseToken, GooglePlayProrationMode? prorationMode)
        {
            var billingFlowParams = GetBillingFlowParamClass().CallStatic<AndroidJavaObject>("newBuilder");

            billingFlowParams = SetObfuscatedAccountIdIfNeeded(billingFlowParams);
            billingFlowParams = SetObfuscatedProfileIdIfNeeded(billingFlowParams);

            billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setSkuDetails", sku);

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

        public void LaunchPriceChangeConfirmationFlow(AndroidJavaObject skuDetails, GooglePriceChangeConfirmationListener listener)
        {
            using var priceChangeFlowParams = MakePriceChangeFlowParams(skuDetails);
            m_BillingClient.Call("launchPriceChangeConfirmationFlow", UnityActivity.GetCurrentActivity(), priceChangeFlowParams, listener);
        }

        AndroidJavaObject MakePriceChangeFlowParams(AndroidJavaObject skuDetails)
        {
            var priceChangeFlowParamsBuilder = GetPriceChangeFlowParamClass().CallStatic<AndroidJavaObject>("newBuilder");
            priceChangeFlowParamsBuilder.Call<AndroidJavaObject>("setSkuDetails", skuDetails).Dispose();
            var priceChangeFlowParams = priceChangeFlowParamsBuilder.Call<AndroidJavaObject>("build");
            return priceChangeFlowParams;
        }
    }
}
