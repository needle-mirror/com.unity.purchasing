using System;
using Uniject;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// The internal counterpart to <see cref="ExternalBillingProgramClient"/>
    /// This class is for implementation details that we don't want to expose publicly
    /// </summary>
    internal class ExternalBillingProgramClientInternal : BillingClientBase, IExternalBillingProgramClientInternal
    {
        // From <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient.BillingProgram#EXTERNAL_CONTENT_LINK()">
        const int k_EXTERNAL_CONTENT_LINK = 1;

        const string k_BillingProgramReportingDetailsParamsClassName =
            "com.android.billingclient.api.BillingProgramReportingDetailsParams";
        const string k_LaunchExternalLinkParamsClassName =
            "com.android.billingclient.api.LaunchExternalLinkParams";
        const string k_AndroidUriClassName = "android.net.Uri";

        static AndroidJavaClass s_BillingProgramReportingDetailsParamsClass;
        static AndroidJavaClass s_LaunchExternalLinkParamsClass;
        static AndroidJavaClass s_AndroidUriClass;

        [Preserve]
        internal ExternalBillingProgramClientInternal(IUtil util, ITelemetryDiagnostics telemetryDiagnostics) :
            base(util, telemetryDiagnostics)
        {
            using var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>(
                "newBuilder",
                UnityActivity.GetCurrentActivity()
            );
            builder.Call<AndroidJavaObject>("enableBillingProgram", k_EXTERNAL_CONTENT_LINK).Dispose();
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        public void IsBillingProgramAvailableAsync(Action<IGoogleBillingResult> onBillingProgramAvailabilityResponse)
        {
            m_BillingClient.Call(
                "isBillingProgramAvailableAsync",
                k_EXTERNAL_CONTENT_LINK,
                new BillingProgramAvailabilityListener(onBillingProgramAvailabilityResponse, m_Util)
            );
        }

        public void LaunchExternalLink(
            string externalLinkUrl,
            Action<IGoogleBillingResult> onLaunchExternalLinkResponse,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            using var newLaunchExternalLinkParams = launchExternalLinkParams(externalLinkUrl, linkType, launchMode);

            m_BillingClient.Call(
                "launchExternalLink",
                UnityActivity.GetCurrentActivity(),
                newLaunchExternalLinkParams,
                new LaunchExternalLinkResponseListener(onLaunchExternalLinkResponse, m_Util)
            );
        }

        AndroidJavaObject launchExternalLinkParams(
            string externalLinkUrl,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            using var uri = GetAndroidUriClass()
                .CallStatic<AndroidJavaObject>("parse", externalLinkUrl);

            using var builder = GetLaunchExternalLinkParamsClass()
                .CallStatic<AndroidJavaObject>("newBuilder");

            builder.Call<AndroidJavaObject>("setBillingProgram", k_EXTERNAL_CONTENT_LINK).Dispose();
            builder.Call<AndroidJavaObject>("setLinkUri", uri).Dispose();
            builder.Call<AndroidJavaObject>("setLinkType", (int)linkType).Dispose();
            builder.Call<AndroidJavaObject>("setLaunchMode", (int)launchMode).Dispose();

            var launchExternalLinkParams = builder.Call<AndroidJavaObject>("build");
            return launchExternalLinkParams;
        }

        public void CreateBillingProgramReportingDetailsAsync(Action<IGoogleBillingResult, string> onCreateBillingProgramReportingDetailsResponse)
        {
            using var builder = GetBillingProgramReportingDetailsParamsClass()
                .CallStatic<AndroidJavaObject>("newBuilder");
            
            builder.Call<AndroidJavaObject>("setBillingProgram", k_EXTERNAL_CONTENT_LINK).Dispose();

            using var reportingDetailsParams = builder.Call<AndroidJavaObject>("build");

            m_BillingClient.Call("createBillingProgramReportingDetailsAsync", reportingDetailsParams, new BillingProgramReportingDetailsListener(onCreateBillingProgramReportingDetailsResponse, m_Util));
        }

        static AndroidJavaClass GetBillingProgramReportingDetailsParamsClass()
        {
            s_BillingProgramReportingDetailsParamsClass ??= new AndroidJavaClass(k_BillingProgramReportingDetailsParamsClassName);
            return s_BillingProgramReportingDetailsParamsClass;
        }

        static AndroidJavaClass GetLaunchExternalLinkParamsClass()
        {
            s_LaunchExternalLinkParamsClass ??= new AndroidJavaClass(k_LaunchExternalLinkParamsClassName);
            return s_LaunchExternalLinkParamsClass;
        }

        static AndroidJavaClass GetAndroidUriClass()
        {
            s_AndroidUriClass ??= new AndroidJavaClass(k_AndroidUriClassName);
            return s_AndroidUriClass;
        }
    }
}
