using System;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class BillingConfigResponseListener : AndroidJavaProxy
    {
        const string k_ClassName = "com.android.billingclient.api.BillingConfigResponseListener";

        readonly Action<IGoogleBillingResult, string> m_OnResponse;
        readonly IUtil m_Util;

        internal BillingConfigResponseListener(Action<IGoogleBillingResult, string> onResponse, IUtil util)
            : base(k_ClassName)
        {
            m_OnResponse = onResponse;
            m_Util = util;
        }

        [Preserve]
        public void onBillingConfigResponse(AndroidJavaObject billingResult, AndroidJavaObject billingConfig)
        {
            m_Util.RunOnMainThread(() =>
            {
                var result = new GoogleBillingResult(billingResult);
                string countryCode = null;
                if (result.responseCode == GoogleBillingResponseCode.Ok && billingConfig != null)
                {
                    countryCode = billingConfig.Call<string>("getCountryCode");
                }

                m_OnResponse(result, countryCode);
                billingResult.Dispose();
                if (billingConfig != null)
                {
                    billingConfig.Dispose();
                }
            });
        }
    }
}
