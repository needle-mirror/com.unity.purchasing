using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
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

        static AndroidJavaClass GetSkuDetailsParamClass()
        {
            return new AndroidJavaClass(k_AndroidSkuDetailsParamClassName);
        }

        const string k_AndroidBillingFlowParamClassName = "com.android.billingclient.api.BillingFlowParams";

        static AndroidJavaClass GetBillingFlowParamClass()
        {
            return new AndroidJavaClass(k_AndroidBillingFlowParamClassName);
        }

        const string k_AndroidSubscriptionUpdateParamClassName = "com.android.billingclient.api.BillingFlowParams$SubscriptionUpdateParams";

        static AndroidJavaClass GetSubscriptionUpdateParamClass()
        {
            return new AndroidJavaClass(k_AndroidSubscriptionUpdateParamClassName);
        }

        const string k_AndroidPriceChangeFlowParamClassName = "com.android.billingclient.api.PriceChangeFlowParams";

        static AndroidJavaClass GetPriceChangeFlowParamClass()
        {
            return new AndroidJavaClass(k_AndroidPriceChangeFlowParamClassName);
        }

        const string k_AndroidConsumeParamsClassName = "com.android.billingclient.api.ConsumeParams";
        static AndroidJavaClass GetConsumeParamsClass()
        {
            return new AndroidJavaClass(k_AndroidConsumeParamsClassName);
        }

        const string k_AndroidAcknowledgePurchaseParamsClassName = "com.android.billingclient.api.AcknowledgePurchaseParams";
        static AndroidJavaClass GetAcknowledgePurchaseParamsClass()
        {
            return new AndroidJavaClass(k_AndroidAcknowledgePurchaseParamsClassName);
        }

        const string k_AndroidBillingClientClassName = "com.android.billingclient.api.BillingClient";

        static AndroidJavaClass GetBillingClientClass()
        {
            return new AndroidJavaClass(k_AndroidBillingClientClassName);
        }

        readonly AndroidJavaObject m_BillingClient;
        string m_ObfuscatedAccountId;
        string m_ObfuscatedProfileId;
        readonly IUtil m_Util;

        internal GoogleBillingClient(IGooglePurchaseUpdatedListener googlePurchaseUpdatedListener, IUtil util)
        {
            m_Util = util;
            var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>("newBuilder", UnityActivity.GetCurrentActivity());
            builder = builder.Call<AndroidJavaObject>("setListener", googlePurchaseUpdatedListener);
            builder = builder.Call<AndroidJavaObject>("enablePendingPurchases");
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

        public void QueryPurchasesAsync(string skuType, Action<IGoogleBillingResult, IEnumerable<IAndroidJavaObjectWrapper>> onQueryPurchasesResponse)
        {
            var listener = new GooglePurchasesResponseListener(onQueryPurchasesResponse);
            m_BillingClient.Call("queryPurchasesAsync", skuType, listener);
        }

        public void QuerySkuDetailsAsync(List<string> skus, string type,
            Action<IGoogleBillingResult, List<AndroidJavaObject>> onSkuDetailsResponseAction)
        {
            var skuDetailsParamsBuilder = GetSkuDetailsParamClass().CallStatic<AndroidJavaObject>("newBuilder");
            skuDetailsParamsBuilder = skuDetailsParamsBuilder.Call<AndroidJavaObject>("setSkusList", skus.ToJava());
            skuDetailsParamsBuilder = skuDetailsParamsBuilder.Call<AndroidJavaObject>("setType", type);
            var skuDetailsParams = skuDetailsParamsBuilder.Call<AndroidJavaObject>("build");
            var listener = new SkuDetailsResponseListener(onSkuDetailsResponseAction, m_Util);
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
            var consumeParams = GetConsumeParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            consumeParams = consumeParams.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken);
            consumeParams = consumeParams.Call<AndroidJavaObject>("build");

            m_BillingClient.Call("consumeAsync", consumeParams, new GoogleConsumeResponseListener(onConsume));
        }

        public void AcknowledgePurchase(string purchaseToken, Action<IGoogleBillingResult> onAcknowledge)
        {
            var acknowledgePurchaseParams = GetAcknowledgePurchaseParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            acknowledgePurchaseParams = acknowledgePurchaseParams.Call<AndroidJavaObject>("setPurchaseToken", purchaseToken);
            acknowledgePurchaseParams = acknowledgePurchaseParams.Call<AndroidJavaObject>("build");

            m_BillingClient.Call("acknowledgePurchase", acknowledgePurchaseParams, new GoogleAcknowledgePurchaseListener(onAcknowledge));
        }

        public void LaunchPriceChangeConfirmationFlow(AndroidJavaObject skuDetails, GooglePriceChangeConfirmationListener listener)
        {
            m_BillingClient.Call("launchPriceChangeConfirmationFlow", UnityActivity.GetCurrentActivity(), MakePriceChangeFlowParams(skuDetails), listener);
        }

        AndroidJavaObject MakePriceChangeFlowParams(AndroidJavaObject skuDetails)
        {
            var priceChangeFlowParams = GetPriceChangeFlowParamClass().CallStatic<AndroidJavaObject>("newBuilder");
            priceChangeFlowParams = priceChangeFlowParams.Call<AndroidJavaObject>("setSkuDetails", skuDetails);
            priceChangeFlowParams = priceChangeFlowParams.Call<AndroidJavaObject>("build");
            return priceChangeFlowParams;
        }
    }
}
