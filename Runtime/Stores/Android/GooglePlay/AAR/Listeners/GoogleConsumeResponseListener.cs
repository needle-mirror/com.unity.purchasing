using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class ConsumeResponseListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/ConsumeResponseListener">See more</a>
    /// </summary>
    class GoogleConsumeResponseListener : AndroidJavaProxy
    {
        const string k_AndroidConsumeResponseListenerClassName = "com.android.billingclient.api.ConsumeResponseListener";
        readonly Action<IGoogleBillingResult> m_OnConsumeResponse;
        readonly IUtil m_Util;

        internal GoogleConsumeResponseListener(Action<IGoogleBillingResult> onConsumeResponseAction, IUtil util)
            : base(k_AndroidConsumeResponseListenerClassName)
        {
            m_OnConsumeResponse = onConsumeResponseAction;
            m_Util = util;
        }

        [Preserve]
        void onConsumeResponse(AndroidJavaObject billingResult, string purchaseToken)
        {
            m_Util.RunOnMainThread(() =>
            {
                m_OnConsumeResponse(new GoogleBillingResult(billingResult));
                billingResult.Dispose();
            });
        }
    }
}
