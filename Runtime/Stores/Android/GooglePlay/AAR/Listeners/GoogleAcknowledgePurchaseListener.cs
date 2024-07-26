using System;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class AcknowledgePurchaseResponseListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/AcknowledgePurchaseResponseListener">See more</a>
    /// </summary>
    class GoogleAcknowledgePurchaseListener : AndroidJavaProxy
    {
        const string k_AndroidAcknowledgePurchaseResponseListenerClassName =
            "com.android.billingclient.api.AcknowledgePurchaseResponseListener";

        readonly Action<IGoogleBillingResult> m_OnAcknowledgePurchaseResponse;
        readonly IUtil m_Util;

        internal GoogleAcknowledgePurchaseListener(Action<IGoogleBillingResult> onAcknowledgePurchaseResponseAction,
            IUtil util)
            : base(k_AndroidAcknowledgePurchaseResponseListenerClassName)
        {
            m_OnAcknowledgePurchaseResponse = onAcknowledgePurchaseResponseAction;
            m_Util = util;
        }

        [Preserve]
        void onAcknowledgePurchaseResponse(AndroidJavaObject billingResult)
        {
            m_Util.RunOnMainThread(() =>
            {
                m_OnAcknowledgePurchaseResponse(new GoogleBillingResult(billingResult));
                billingResult.Dispose();
            });
        }
    }
}
