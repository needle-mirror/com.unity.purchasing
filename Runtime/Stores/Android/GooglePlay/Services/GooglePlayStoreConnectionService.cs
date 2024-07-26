using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreConnectionService : IGooglePlayStoreConnectionService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IBillingClientStateListener m_BillingClientStateListener;
        IStoreConnectCallback m_ConnectCallback;

        [Preserve]
        public GooglePlayStoreConnectionService(IGoogleBillingClient billingClient,
            IBillingClientStateListener billingClientStateListener)
        {
            m_BillingClient = billingClient;
            m_BillingClientStateListener = billingClientStateListener;
        }

        public void Connect()
        {
            m_BillingClientStateListener.RegisterOnConnected(OnConnected);
            m_BillingClientStateListener.RegisterOnDisconnected(OnDisconnected);
            m_BillingClient.StartConnection(m_BillingClientStateListener);
        }

        public bool IsReady()
        {
            return m_BillingClient.IsReady();
        }

        public GoogleBillingConnectionState CheckConnectionState()
        {
            return m_BillingClient.GetConnectionState();
        }

        public void SetConnectionCallback(IStoreConnectCallback storeConnectCallback)
        {
            m_ConnectCallback = storeConnectCallback;
        }

        void OnConnected()
        {
            m_ConnectCallback.OnStoreConnectionSucceeded();
        }

        void OnDisconnected(GoogleBillingResponseCode responseCode)
        {
            m_ConnectCallback.OnStoreConnectionFailed(new StoreConnectionFailureDescription("GooglePlayStore connection failed", true));
        }
    }
}
