using System;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    /// <summary>
    /// The internal counterpart to <see cref="ExternalBillingProgramClient"/>
    /// This class is for implementation details that we don't want to expose publicly
    /// </summary>
    sealed class ExternalBillingProgramClientInternal : BillingClientBase
    {
        // From <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClient.BillingProgram#EXTERNAL_CONTENT_LINK()">
        const int k_EXTERNAL_CONTENT_LINK = 1;

        [Preserve]
        internal ExternalBillingProgramClientInternal()
        {
            using var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>(
                "newBuilder",
                UnityActivity.GetCurrentActivity()
            );
            builder.Call<AndroidJavaObject>("enableBillingProgram", k_EXTERNAL_CONTENT_LINK).Dispose();
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        internal void IsBillingProgramAvailableAsync(Action<IGoogleBillingResult> onBillingProgramAvailabilityResponse)
        {
            m_BillingClient.Call(
                "isBillingProgramAvailableAsync",
                k_EXTERNAL_CONTENT_LINK,
                new BillingProgramAvailabilityListener(onBillingProgramAvailabilityResponse)
            );
        }

        internal void LaunchExternalLink(
            string externalLinkUrl,
            Action<IGoogleBillingResult> onLaunchExternalLinkResponse,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            m_BillingClient.Call(
                "launchExternalLink",
                UnityActivity.GetCurrentActivity(),
                launchExternalLinkParams(externalLinkUrl, linkType, launchMode),
                new LaunchExternalLinkResponseListener(onLaunchExternalLinkResponse)
            );
        }

        AndroidJavaObject launchExternalLinkParams(
            string externalLinkUrl,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            var uri = new AndroidJavaClass("android.net.Uri")
                .CallStatic<AndroidJavaObject>("parse", externalLinkUrl);

            var builder = new AndroidJavaClass("com.android.billingclient.api.LaunchExternalLinkParams")
                .CallStatic<AndroidJavaObject>("newBuilder")
                .Call<AndroidJavaObject>("setBillingProgram", k_EXTERNAL_CONTENT_LINK)
                .Call<AndroidJavaObject>("setLinkUri", uri)
                .Call<AndroidJavaObject>("setLinkType", (int)linkType)
                .Call<AndroidJavaObject>("setLaunchMode", (int)launchMode);

            var launchExternalLinkParams = builder.Call<AndroidJavaObject>("build");
            return launchExternalLinkParams;
        }

        internal void CreateBillingProgramReportingDetailsAsync(Action<IGoogleBillingResult, string> onCreateBillingProgramReportingDetailsResponse)
        {
            var paramsClass = new AndroidJavaClass("com.android.billingclient.api.BillingProgramReportingDetailsParams");
            var builder = paramsClass.CallStatic<AndroidJavaObject>("newBuilder");

            var reportingDetailsParams = builder
                .Call<AndroidJavaObject>("setBillingProgram", k_EXTERNAL_CONTENT_LINK)
                .Call<AndroidJavaObject>("build");

            m_BillingClient.Call("createBillingProgramReportingDetailsAsync", reportingDetailsParams, new BillingProgramReportingDetailsListener(onCreateBillingProgramReportingDetailsResponse));
        }
    }
}
