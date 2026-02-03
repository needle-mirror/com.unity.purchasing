using System;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class BillingProgramReportingDetailsListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingProgramReportingDetailsListener">See more</a>
    /// </summary>
    class BillingProgramReportingDetailsListener : AndroidJavaProxy
    {
        const string k_AndroidBillingProgramReportingDetailsListenerClassName =
            "com.android.billingclient.api.BillingProgramReportingDetailsListener";

        readonly Action<IGoogleBillingResult, string> m_OnCreateBillingProgramReportingDetailsResponseAction;

        internal BillingProgramReportingDetailsListener(Action<IGoogleBillingResult, string> onCreateBillingProgramReportingDetailsResponseAction)
            : base(k_AndroidBillingProgramReportingDetailsListenerClassName)
        {
            m_OnCreateBillingProgramReportingDetailsResponseAction = onCreateBillingProgramReportingDetailsResponseAction;
        }

        [Preserve]
        public void onCreateBillingProgramReportingDetailsResponse(AndroidJavaObject billingResult, AndroidJavaObject billingProgramReportingDetails)
        {
            var googleBillingResult = new GoogleBillingResult(billingResult);
            var externalTransactionToken = string.Empty;
            if (googleBillingResult.responseCode == GoogleBillingResponseCode.Ok)
                externalTransactionToken = billingProgramReportingDetails.Call<string>("getExternalTransactionToken");
            m_OnCreateBillingProgramReportingDetailsResponseAction(new GoogleBillingResult(billingResult), externalTransactionToken);
            billingResult.Dispose();
            billingProgramReportingDetails?.Dispose();
        }
    }
}
