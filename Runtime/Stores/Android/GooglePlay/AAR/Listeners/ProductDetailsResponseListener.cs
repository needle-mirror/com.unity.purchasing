#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class ProductDetailsResponseListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/ProductDetailsResponseListener">See more</a>
    /// </summary>
    class ProductDetailsResponseListener : AndroidJavaProxy
    {
        const string k_AndroidProductDetailsResponseListenerClassName = "com.android.billingclient.api.ProductDetailsResponseListener";
        readonly Action<IGoogleBillingResult, List<AndroidJavaObject>> m_OnProductDetailsResponse;
        readonly IUtil m_Util;
        readonly ITelemetryDiagnostics m_TelemetryDiagnostics;

        internal ProductDetailsResponseListener(
            Action<IGoogleBillingResult, List<AndroidJavaObject>> onProductDetailsResponseAction, IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics)
            : base(k_AndroidProductDetailsResponseListenerClassName)
        {
            m_OnProductDetailsResponse = onProductDetailsResponseAction;
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        [Preserve]
        public void onProductDetailsResponse(AndroidJavaObject billingResult, AndroidJavaObject? productDetails)
        {
            m_Util.RunOnMainThread(() =>
            {
                List<AndroidJavaObject>? productDetailsList = null;

                try
                {
                    productDetailsList = productDetails.Enumerate<AndroidJavaObject>().ToList();
                    m_OnProductDetailsResponse(new GoogleBillingResult(billingResult), productDetailsList);
                }
                catch (Exception ex)
                {
                    m_TelemetryDiagnostics.SendDiagnostic(TelemetryDiagnosticNames.SkuDetailsResponseError, ex);

                }

#if UNITY_2021_2_OR_NEWER
                if (productDetailsList != null)
                {
                    foreach (var obj in productDetailsList)
                    {
                        obj?.Dispose();
                    }
                }
#endif

                billingResult.Dispose();
                productDetails?.Dispose();
            });
        }
    }
}
