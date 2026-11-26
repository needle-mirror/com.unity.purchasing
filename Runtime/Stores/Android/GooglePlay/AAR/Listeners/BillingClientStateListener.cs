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
        Action<GoogleBillingResponseCode, string> m_Disconnect;
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

        public void RegisterOnDisconnected(Action<GoogleBillingResponseCode, string> onDisconnected)
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
            HandleBillingSetupFinished(new GoogleBillingResult(billingResult));
            billingResult.Dispose();
        }

        internal void HandleBillingSetupFinished(IGoogleBillingResult result)
        {
            if (result.responseCode == GoogleBillingResponseCode.Ok)
            {
                m_OnConnected();
            }
            else
            {
                m_Disconnect(result.responseCode, result.debugMessage);
            }
        }

        [Preserve]
        public void onBillingServiceDisconnected()
        {
            m_Util.RunOnMainThread(() => m_Disconnect(GoogleBillingResponseCode.ServiceDisconnected, null));
        }
    }
}
