using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// This is C# representation of the Java Class BillingClient
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient">See more</a>
    /// </summary>
    sealed class BillingClient : BillingClientBase, IBillingClient
    {
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;
        const string k_AndroidPendingPurchasesParamsClassName = "com.android.billingclient.api.PendingPurchasesParams";
        static AndroidJavaClass s_PendingPurchasesParamsClass;

        internal BillingClient(IGooglePurchaseUpdatedListener googlePurchaseUpdatedListener, IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics) : base()
        {
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
            using var builder = GetBillingClientClass()
                .CallStatic<AndroidJavaObject>("newBuilder", UnityActivity.GetCurrentActivity());
            builder.Call<AndroidJavaObject>("setListener", googlePurchaseUpdatedListener).Dispose();
            builder.Call<AndroidJavaObject>("enablePendingPurchases", PendingPurchasesParams()).Dispose();
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        public void QueryPurchasesAsync(
            string productType,
            Action<IGoogleBillingResult, IEnumerable<AndroidJavaObject>> onQueryPurchasesResponse
        )
        {
            var queryPurchaseParams = QueryPurchasesParams(productType);
            var listener = new GooglePurchasesResponseListener(onQueryPurchasesResponse);

            m_BillingClient.Call("queryPurchasesAsync", queryPurchaseParams, listener);
        }

        public void QueryProductDetailsAsync(List<string> products, string type, Action<IGoogleBillingResult, List<AndroidJavaObject>> onProductDetailsResponseAction)
        {
            using var queryProductDetailsParams = QueryProductDetailsParams(products, type);
            var productDetailsResponseListener = new ProductDetailsResponseListener(onProductDetailsResponseAction, m_Util, m_TelemetryDiagnostics);
            m_BillingClient.Call("queryProductDetailsAsync", queryProductDetailsParams, productDetailsResponseListener);
        }

        public AndroidJavaObject LaunchBillingFlow(AndroidJavaObject productDetails, string oldPurchaseToken,
            GooglePlayReplacementMode? replacementMode)
        {
            // We currently only support 1 base plan so we can safely get the first one.
            // Once we support multiple, this will need to be updated.
            using var subscriptionOfferDetails = productDetails.Call<AndroidJavaObject>("getSubscriptionOfferDetails");
            var firstSubscriptionOfferDetails = subscriptionOfferDetails?.Enumerate().ToList().FirstOrDefault();
            var offerToken = firstSubscriptionOfferDetails?.Call<string>("getOfferToken");

            using var productDetailsParamsBuilder = GetProductDetailsParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            productDetailsParamsBuilder.Call<AndroidJavaObject>("setProductDetails", productDetails).Dispose();
            if (offerToken != null)
            {
                productDetailsParamsBuilder.Call<AndroidJavaObject>("setOfferToken", offerToken).Dispose();
            }
            using var productDetailsParams =  productDetailsParamsBuilder.Call<AndroidJavaObject>("build");
            var productDetailsParamsList = new List<AndroidJavaObject> { productDetailsParams }.ToJava();

            return m_BillingClient.Call<AndroidJavaObject>(
                "launchBillingFlow",
                UnityActivity.GetCurrentActivity(),
                MakeBillingFlowParams(productDetailsParamsList, oldPurchaseToken, replacementMode)
            );
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

        public void SetObfuscationAccountId(string obfuscationAccountId)
        {
            m_ObfuscatedAccountId = obfuscationAccountId;
        }

        public void SetObfuscationProfileId(string obfuscationProfileId)
        {
            m_ObfuscatedProfileId = obfuscationProfileId;
        }

#region Android Class Names
        const string k_AndroidQueryPurchasesParamsClassName = "com.android.billingclient.api.QueryPurchasesParams";
        const string k_AndroidQueryProductDetailsParamsClassName = "com.android.billingclient.api.QueryProductDetailsParams";
        const string k_AndroidProductClassName = "com.android.billingclient.api.QueryProductDetailsParams$Product";
        const string k_AndroidProductDetailsParamsClassName = "com.android.billingclient.api.BillingFlowParams$ProductDetailsParams";
        const string k_AndroidBillingFlowParamClassName = "com.android.billingclient.api.BillingFlowParams";
        const string k_AndroidSubscriptionUpdateParamClassName = "com.android.billingclient.api.BillingFlowParams$SubscriptionUpdateParams";
        const string k_AndroidConsumeParamsClassName = "com.android.billingclient.api.ConsumeParams";
        const string k_AndroidAcknowledgePurchaseParamsClassName = "com.android.billingclient.api.AcknowledgePurchaseParams";
#endregion

#region Static Class Declarations
        static AndroidJavaClass s_QueryPurchasesParamsClass;
        static AndroidJavaClass s_AndroidQueryProductDetailsParamsClassName;
        static AndroidJavaClass s_AndroidProductClassName;
        static AndroidJavaClass s_ProductDetailsParamsClass;
        static AndroidJavaClass s_BillingFlowParamsClass;
        static AndroidJavaClass s_SubscriptionUpdateParamsClass;
        static AndroidJavaClass s_ConsumeParamsClass;
        static AndroidJavaClass s_AcknowledgePurchaseParamsClass;
#endregion

#region Helpers
        static AndroidJavaObject QueryPurchasesParams(string productType)
        {
            using var queryProductDetailsParams = GetQueryPurchasesParamsClass()
                .CallStatic<AndroidJavaObject>("newBuilder");
            queryProductDetailsParams.Call<AndroidJavaObject>("setProductType", productType).Dispose();
            return queryProductDetailsParams.Call<AndroidJavaObject>("build");
        }

        static AndroidJavaClass GetQueryPurchasesParamsClass()
        {
            s_QueryPurchasesParamsClass ??= new AndroidJavaClass(k_AndroidQueryPurchasesParamsClassName);
            return s_QueryPurchasesParamsClass;
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

        static AndroidJavaClass GetProductParamsClass()
        {
            s_AndroidProductClassName ??= new AndroidJavaClass(k_AndroidProductClassName);
            return s_AndroidProductClassName;
        }

        static AndroidJavaClass GetProductDetailsParamsClass()
        {
            s_ProductDetailsParamsClass ??= new AndroidJavaClass(k_AndroidProductDetailsParamsClassName);
            return s_ProductDetailsParamsClass;
        }

        AndroidJavaObject MakeBillingFlowParams(AndroidJavaObject productDetailsParamsList, string oldPurchaseToken, GooglePlayReplacementMode? replacementMode)
        {
            var billingFlowParams = GetBillingFlowParamClass().CallStatic<AndroidJavaObject>("newBuilder");

            billingFlowParams = SetObfuscatedAccountIdIfNeeded(billingFlowParams);
            billingFlowParams = SetObfuscatedProfileIdIfNeeded(billingFlowParams);

            billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setProductDetailsParamsList", productDetailsParamsList);

            if (oldPurchaseToken != null && replacementMode != null)
            {
                var subscriptionUpdateParams = BuildSubscriptionUpdateParams(oldPurchaseToken, replacementMode.Value);
                billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("setSubscriptionUpdateParams", subscriptionUpdateParams);
            }

            billingFlowParams = billingFlowParams.Call<AndroidJavaObject>("build");
            return billingFlowParams;
        }

        static AndroidJavaClass GetBillingFlowParamClass()
        {
            s_BillingFlowParamsClass ??= new AndroidJavaClass(k_AndroidBillingFlowParamClassName);
            return s_BillingFlowParamsClass;
        }

        static AndroidJavaClass GetSubscriptionUpdateParamClass()
        {
            s_SubscriptionUpdateParamsClass ??= new AndroidJavaClass(k_AndroidSubscriptionUpdateParamClassName);
            return s_SubscriptionUpdateParamsClass;
        }

        static AndroidJavaObject BuildSubscriptionUpdateParams(string oldPurchaseToken, GooglePlayReplacementMode replacementMode)
        {
            var subscriptionUpdateParams = GetSubscriptionUpdateParamClass().CallStatic<AndroidJavaObject>("newBuilder");

            subscriptionUpdateParams = subscriptionUpdateParams.Call<AndroidJavaObject>("setSubscriptionReplacementMode", (int)replacementMode);
            subscriptionUpdateParams = subscriptionUpdateParams.Call<AndroidJavaObject>("setOldPurchaseToken", oldPurchaseToken);

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

        static AndroidJavaClass GetConsumeParamsClass()
        {
            s_ConsumeParamsClass ??= new AndroidJavaClass(k_AndroidConsumeParamsClassName);
            return s_ConsumeParamsClass;
        }

        static AndroidJavaClass GetAcknowledgePurchaseParamsClass()
        {
            s_AcknowledgePurchaseParamsClass ??= new AndroidJavaClass(k_AndroidAcknowledgePurchaseParamsClassName);
            return s_AcknowledgePurchaseParamsClass;
        }

        static AndroidJavaObject PendingPurchasesParams()
        {
            using var queryProductDetailsParams = GetPendingPurchasesParamsClass().CallStatic<AndroidJavaObject>("newBuilder");
            queryProductDetailsParams.Call<AndroidJavaObject>("enableOneTimeProducts").Dispose();
            return queryProductDetailsParams.Call<AndroidJavaObject>("build");
        }

        static AndroidJavaClass GetQueryProductDetailsParamsParamsClass()
        {
            s_AndroidQueryProductDetailsParamsClassName ??= new AndroidJavaClass(k_AndroidQueryProductDetailsParamsClassName);
            return s_AndroidQueryProductDetailsParamsClassName;
        }

        static AndroidJavaClass GetPendingPurchasesParamsClass()
        {
            s_PendingPurchasesParamsClass ??= new AndroidJavaClass(k_AndroidPendingPurchasesParamsClassName);
            return s_PendingPurchasesParamsClass;
        }
#endregion
    }
}
