using System;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class LaunchExternalLinkResponseListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/LaunchExternalLinkResponseListener">See more</a>
    /// </summary>
    class LaunchExternalLinkResponseListener : AndroidJavaProxy
    {
        const string k_AndroidLaunchExternalLinkResponseListenerClassName =
            "com.android.billingclient.api.LaunchExternalLinkResponseListener";

        readonly IUtil m_Util;
        readonly Action<IGoogleBillingResult> m_OnBillingProgramAvailabilityResponseAction;

        internal LaunchExternalLinkResponseListener(Action<IGoogleBillingResult> onLaunchExternalLinkResponseAction, IUtil util)
            : base(k_AndroidLaunchExternalLinkResponseListenerClassName)
        {
            m_OnBillingProgramAvailabilityResponseAction = onLaunchExternalLinkResponseAction;
            m_Util = util;
        }

        [Preserve]
        public void onLaunchExternalLinkResponse(AndroidJavaObject billingResult)
        {
            m_Util.RunOnMainThread(() =>
            {
                m_OnBillingProgramAvailabilityResponseAction(new GoogleBillingResult(billingResult));
                billingResult.Dispose();
            });
        }
    }
}
