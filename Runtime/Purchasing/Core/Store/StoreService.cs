#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The service responsible for connecting to a store and its extensions.
    /// </summary>
    class StoreService : IStoreService
    {
        readonly IStoreConnectUseCase m_StoreConnectUseCase;
        readonly IStoreWrapper m_StoreWrapper;

        internal StoreService(IStoreConnectUseCase connectUseCase, IStoreWrapper storeWrapper)
        {
            m_StoreConnectUseCase = connectUseCase;
            m_StoreWrapper = storeWrapper;
        }

        public IAppleStoreExtendedService? Apple => this as IAppleStoreExtendedService;

        public IGooglePlayStoreExtendedService? Google => this as IGooglePlayStoreExtendedService;

        public IPaymentProvidersExtendedService? PaymentProviders => this as IPaymentProvidersExtendedService;

        public Task Connect()
        {
            return m_StoreConnectUseCase.Connect();
        }

        public ConnectionState GetConnectionState()
        {
            return m_StoreWrapper.GetStoreConnectionState();
        }

        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy)
        {
            m_StoreConnectUseCase.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy ?? new NoRetriesPolicy());
        }

        public event Action<StoreConnectionFailureDescription>? OnStoreDisconnected
        {
            add => m_StoreConnectUseCase.OnStoreDisconnection += value;
            remove => m_StoreConnectUseCase.OnStoreDisconnection -= value;
        }

        public event Action? OnStoreConnected
        {
            add => m_StoreConnectUseCase.OnStoreConnection += value;
            remove => m_StoreConnectUseCase.OnStoreConnection -= value;
        }

        public event Action? OnAuthAccountChanged
        {
            add => m_StoreConnectUseCase.OnAuthAccountChanged += value;
            remove => m_StoreConnectUseCase.OnAuthAccountChanged -= value;
        }
    }
}
