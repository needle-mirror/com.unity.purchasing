using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing.GoogleBilling.Models
{
    internal abstract class BillingClientBase : IBillingClientBase
    {
        public AndroidJavaObject m_BillingClient { get; protected set; }
        protected string m_ObfuscatedAccountId;
        protected string m_ObfuscatedProfileId;
        const string k_AndroidBillingClientClassName = "com.android.billingclient.api.BillingClient";
        static AndroidJavaClass s_BillingClientClass;

        protected BillingClientBase()
        {
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
