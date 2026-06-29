using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Stores;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreConnectionService : IGooglePlayStoreConnectionService
    {
        readonly IBillingClient m_BillingClient;
        readonly IBillingClientStateListener m_BillingClientStateListener;
        readonly IStoreLocationContext m_StoreLocationContext;
        IStoreConnectCallback m_ConnectCallback;

        [Preserve]
        public GooglePlayStoreConnectionService(IBillingClient billingClient,
            IBillingClientStateListener billingClientStateListener,
            IStoreLocationContext storeLocationContext)
        {
            m_BillingClient = billingClient;
            m_BillingClientStateListener = billingClientStateListener;
            m_StoreLocationContext = storeLocationContext;
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
            m_BillingClient.GetBillingConfigAsync(OnBillingConfigReceived);
        }

        void OnBillingConfigReceived(IGoogleBillingResult result, string countryCode)
        {
            if (result.responseCode == GoogleBillingResponseCode.Ok && !string.IsNullOrEmpty(countryCode))
            {
                m_StoreLocationContext.CountryCode = countryCode;
            }
        }

        void OnDisconnected(GoogleBillingResponseCode responseCode)
        {
            m_ConnectCallback.OnStoreConnectionFailed(new StoreConnectionFailureDescription("GooglePlayStore connection failed", true));
        }
    }
}
