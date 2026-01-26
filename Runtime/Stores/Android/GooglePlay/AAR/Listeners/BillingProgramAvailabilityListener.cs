using System;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class BillingProgramAvailabilityListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingProgramAvailabilityListener">See more</a>
    /// </summary>
    class BillingProgramAvailabilityListener : AndroidJavaProxy
    {
        const string k_AndroidBillingProgramAvailabilityListenerClassName =
            "com.android.billingclient.api.BillingProgramAvailabilityListener";

        readonly Action<IGoogleBillingResult> m_OnBillingProgramAvailabilityResponseAction;

        internal BillingProgramAvailabilityListener(Action<IGoogleBillingResult> onBillingProgramAvailabilityResponseAction)
            : base(k_AndroidBillingProgramAvailabilityListenerClassName)
        {
            m_OnBillingProgramAvailabilityResponseAction = onBillingProgramAvailabilityResponseAction;
        }

        [Preserve]
        public void onBillingProgramAvailabilityResponse(AndroidJavaObject billingResult, AndroidJavaObject billingProgramAvailabilityDetails)
        {
            m_OnBillingProgramAvailabilityResponseAction(new GoogleBillingResult(billingResult));
            billingResult.Dispose();
            billingProgramAvailabilityDetails.Dispose();
        }
    }
}
