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

        void OnDisconnected(GoogleBillingResponseCode responseCode, string debugMessage)
        {
            var message = string.IsNullOrEmpty(debugMessage)
                ? $"GooglePlayStore connection failed - Google BillingResponseCode = {responseCode}"
                : $"GooglePlayStore connection failed: {debugMessage} - Google BillingResponseCode = {responseCode}";

            m_ConnectCallback.OnStoreConnectionFailed(new StoreConnectionFailureDescription(message,
                IsRetryable(responseCode), ShouldAutoRetry(responseCode)));
        }

        /// <summary>
        /// Whether the developer can usefully call Connect again.
        /// False only for the codes Google lists as non-retriable, which cannot succeed without a change to the app.
        /// See https://developer.android.com/google/play/billing/errors
        /// </summary>
        internal static bool IsRetryable(GoogleBillingResponseCode responseCode)
        {
            switch (responseCode)
            {
                case GoogleBillingResponseCode.FeatureNotSupported:
                case GoogleBillingResponseCode.DeveloperError:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Whether IAP reconnects on its own.
        /// True only for transient codes Google says to retry automatically.
        /// Anything else is reported to the developer at once rather than retried.
        /// </summary>
        internal static bool ShouldAutoRetry(GoogleBillingResponseCode responseCode)
        {
            switch (responseCode)
            {
                case GoogleBillingResponseCode.ServiceTimeout:
                case GoogleBillingResponseCode.ServiceDisconnected:
                case GoogleBillingResponseCode.ServiceUnavailable:
                case GoogleBillingResponseCode.FatalError:
                case GoogleBillingResponseCode.NetworkError:
                    return true;
                default:
                    return false;
            }
        }
    }
}
