using System;
using System.Threading.Tasks;
using Stores.Android.GooglePlay.AAR.Models;
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
        const string k_BillingProgramReportingDetailsParamsClassName =
            "com.android.billingclient.api.BillingProgramReportingDetailsParams";
        const string k_LaunchExternalLinkParamsClassName =
            "com.android.billingclient.api.LaunchExternalLinkParams";
        const string k_AndroidUriClassName = "android.net.Uri";

        static AndroidJavaClass s_BillingProgramReportingDetailsParamsClass;
        static AndroidJavaClass s_LaunchExternalLinkParamsClass;
        static AndroidJavaClass s_AndroidUriClass;
        BillingProgram m_BillingProgram;

        internal static readonly BillingProgram[] k_CandidatePrograms =
        {
            BillingProgram.EXTERNAL_CONTENT_LINK,
            // Support for External Offers will be added in a future update
            BillingProgram.EXTERNAL_OFFER,
            // Support for External Payments will be added in a future update
            // BillingProgram.EXTERNAL_PAYMENTS
        };

        [Preserve]
        internal ExternalBillingProgramClientInternal(
            IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics)
            : base(util, telemetryDiagnostics)
        {
            using var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>(
                "newBuilder",
                UnityActivity.GetCurrentActivity()
            );
            foreach (var program in k_CandidatePrograms)
            {
                builder.Call<AndroidJavaObject>("enableBillingProgram", (int)program).Dispose();
            }
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        [Preserve]
        internal ExternalBillingProgramClientInternal(
            IUtil util,
            ITelemetryDiagnostics telemetryDiagnostics,
            BillingProgram billingProgram)
            : base(util, telemetryDiagnostics)
        {
            using var builder = GetBillingClientClass().CallStatic<AndroidJavaObject>(
                "newBuilder",
                UnityActivity.GetCurrentActivity()
            );
            m_BillingProgram = billingProgram;
            builder.Call<AndroidJavaObject>("enableBillingProgram", (int)m_BillingProgram).Dispose();
            m_BillingClient = builder.Call<AndroidJavaObject>("build");
        }

        public void IsBillingProgramAvailableAsync(Action<IGoogleBillingResult> onBillingProgramAvailabilityResponse)
        {
            if (m_BillingProgram != BillingProgram.UNSPECIFIED_BILLING_PROGRAM)
            {
                m_BillingClient.Call(
                    "isBillingProgramAvailableAsync",
                    (int)m_BillingProgram,
                    new BillingProgramAvailabilityListener(onBillingProgramAvailabilityResponse, m_Util)
                );
            }
            else
            {
                CheckCandidatePrograms(onBillingProgramAvailabilityResponse);
            }
        }

        async void CheckCandidatePrograms(Action<IGoogleBillingResult> callback)
        {
            IGoogleBillingResult lastResult = null;

            foreach (var program in k_CandidatePrograms)
            {
                var tcs = new TaskCompletionSource<IGoogleBillingResult>();
                m_BillingClient.Call(
                    "isBillingProgramAvailableAsync",
                    (int)program,
                    new BillingProgramAvailabilityListener(result => tcs.TrySetResult(result), m_Util)
                );

                var result = await tcs.Task;
                if (result.responseCode == GoogleBillingResponseCode.Ok)
                {
                    m_BillingProgram = program;
                    callback(result);
                    return;
                }

                lastResult = result;
            }

            callback(lastResult);
        }

        public void LaunchExternalLink(
            string externalLinkUrl,
            Action<IGoogleBillingResult> onLaunchExternalLinkResponse,
            LinkType linkType,
            LaunchMode launchMode
        )
        {
            if (m_BillingProgram == BillingProgram.UNSPECIFIED_BILLING_PROGRAM)
            {
                onLaunchExternalLinkResponse(new GoogleBillingResult(
                    GoogleBillingResponseCode.BillingUnavailable,
                    "Billing program is unspecified; call IsBillingProgramAvailableAsync first to resolve a candidate program."));
                return;
            }

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

            builder.Call<AndroidJavaObject>("setBillingProgram", (int)m_BillingProgram).Dispose();
            builder.Call<AndroidJavaObject>("setLinkUri", uri).Dispose();
            builder.Call<AndroidJavaObject>("setLinkType", (int)linkType).Dispose();
            builder.Call<AndroidJavaObject>("setLaunchMode", (int)launchMode).Dispose();

            var launchExternalLinkParams = builder.Call<AndroidJavaObject>("build");
            return launchExternalLinkParams;
        }

        public void CreateBillingProgramReportingDetailsAsync(Action<IGoogleBillingResult, string> onCreateBillingProgramReportingDetailsResponse)
        {
            if (m_BillingProgram == BillingProgram.UNSPECIFIED_BILLING_PROGRAM)
            {
                onCreateBillingProgramReportingDetailsResponse(
                    new GoogleBillingResult(
                        GoogleBillingResponseCode.BillingUnavailable,
                        "Billing program is unspecified; call IsBillingProgramAvailableAsync first to resolve a candidate program."),
                    null);
                return;
            }

            using var builder = GetBillingProgramReportingDetailsParamsClass()
                .CallStatic<AndroidJavaObject>("newBuilder");

            builder.Call<AndroidJavaObject>("setBillingProgram", (int)m_BillingProgram).Dispose();

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
