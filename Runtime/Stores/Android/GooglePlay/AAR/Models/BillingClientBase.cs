using Uniject;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    internal abstract class BillingClientBase : IBillingClientBase
    {
        public AndroidJavaObject m_BillingClient { get; protected set; }
        protected string m_ObfuscatedAccountId;
        protected string m_ObfuscatedProfileId;
        protected internal readonly IUtil m_Util;
        protected readonly ITelemetryDiagnostics m_TelemetryDiagnostics;
        const string k_AndroidBillingClientClassName = "com.android.billingclient.api.BillingClient";
        static AndroidJavaClass s_BillingClientClass;

        protected BillingClientBase(IUtil util, ITelemetryDiagnostics telemetryDiagnostics)
        {
            m_Util = util;
            m_TelemetryDiagnostics = telemetryDiagnostics;
        }

        public void StartConnection(IBillingClientStateListener billingClientStateListener)
        {
            m_BillingClient.Call("startConnection", billingClientStateListener);
        }

        public void EndConnection()
        {
            m_BillingClient.Call("endConnection");
        }

        public bool IsReady()
        {
            return m_BillingClient.Call<bool>("isReady");
        }

        public GoogleBillingConnectionState GetConnectionState()
        {
            return (GoogleBillingConnectionState)m_BillingClient.Call<int>("getConnectionState");
        }

        internal static AndroidJavaClass GetBillingClientClass()
        {
            s_BillingClientClass ??= new AndroidJavaClass(k_AndroidBillingClientClassName);
            return s_BillingClientClass;
        }
    }
}
