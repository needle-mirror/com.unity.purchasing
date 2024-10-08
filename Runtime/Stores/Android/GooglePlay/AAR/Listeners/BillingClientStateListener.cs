using System;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is C# representation of the Java Class BillingClientStateListener
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/BillingClientStateListener">See more</a>
    /// </summary>
    class BillingClientStateListener : AndroidJavaProxy, IBillingClientStateListener
    {
        const string k_AndroidBillingClientStateListenerClassName =
            "com.android.billingclient.api.BillingClientStateListener";

        Action m_OnConnected;
        Action<GoogleBillingResponseCode> m_Disconnect;
        readonly IUtil m_Util;

        internal BillingClientStateListener(IUtil util)
            : base(k_AndroidBillingClientStateListenerClassName)
        {
            m_Util = util;
        }

        public void RegisterOnConnected(Action onConnected)
        {
            m_OnConnected = onConnected;
        }

        public void RegisterOnDisconnected(Action<GoogleBillingResponseCode> onDisconnected)
        {
            m_Disconnect = onDisconnected;
        }

        [Preserve]
        public void onBillingSetupFinished(AndroidJavaObject billingResult)
        {
            m_Util.RunOnMainThread(() => HandleBillingSetupFinished(billingResult));
        }

        void HandleBillingSetupFinished(AndroidJavaObject billingResult)
        {
            IGoogleBillingResult result = new GoogleBillingResult(billingResult);
            if (result.responseCode == GoogleBillingResponseCode.Ok)
            {
                m_OnConnected();
            }
            else
            {
                m_Disconnect(result.responseCode);
            }

            billingResult.Dispose();
        }

        [Preserve]
        public void onBillingServiceDisconnected()
        {
            m_Util.RunOnMainThread(() => m_Disconnect(GoogleBillingResponseCode.ServiceDisconnected));
        }
    }
}
