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

        internal StoreService(IStoreConnectUseCase connectUseCase)
        {
            m_StoreConnectUseCase = connectUseCase;
        }

        public IAppleStoreExtendedService? Apple => this as IAppleStoreExtendedService;

        public IGooglePlayStoreExtendedService? Google => this as IGooglePlayStoreExtendedService;

        public Task Connect()
        {
            return m_StoreConnectUseCase.Connect();
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
    }
}
