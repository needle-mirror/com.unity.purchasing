#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The service responsible for connecting to a store and its extensions.
    /// </summary>
    internal class StoreService : IStoreService
    {
        readonly IStoreConnectUseCase m_StoreConnectUseCase;
        readonly IRetryPolicy? m_RetryPolicy;

        internal StoreService(IStoreConnectUseCase connectUseCase, IRetryPolicy? defaultConnectionRetryPolicy)
        {
            m_StoreConnectUseCase = connectUseCase;
            m_RetryPolicy = defaultConnectionRetryPolicy;
        }

        public IAppleStoreExtendedService? Apple => this as IAppleStoreExtendedService;

        public IGooglePlayStoreExtendedService? Google => this as IGooglePlayStoreExtendedService;

        public IAmazonAppsStoreExtendedService? Amazon => this as IAmazonAppsStoreExtendedService;

        /// <summary>
        /// Initiates a connection to the store.
        /// </summary>
        /// <returns>Return a handle to the initialization operation.</returns>
        /// <exception cref="StoreConnectionException">Throws an exception if the connection fails.</exception>
        public Task ConnectAsync()
        {
            return m_StoreConnectUseCase.Connect();
        }

        /// <summary>
        /// Set a custom reconnection policy when the store disconnects.
        /// </summary>
        /// <param name="retryPolicy">The policy that will be used to determine if reconnection should be attempted.</param>
        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy)
        {
            m_StoreConnectUseCase.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy ?? new NoRetriesPolicy());
        }

        /// <summary>
        /// Add an action to be called when connection is lost to the current store.
        /// </summary>
        /// <param name="onStoreDisconnected">The action to be added to the list of callbacks.</param>
        public void AddOnStoreDisconnectedAction(Action<StoreConnectionFailureDescription> onStoreDisconnected)
        {
            m_StoreConnectUseCase.OnStoreDisconnection -= onStoreDisconnected;
            m_StoreConnectUseCase.OnStoreDisconnection += onStoreDisconnected;
        }

        /// <summary>
        /// Remove an action to be called when connection is lost to the current store.
        /// </summary>
        /// <param name="onStoreDisconnected">The action to be removed from the list of callbacks.</param>
        public void RemoveOnStoreDisconnectedAction(Action<StoreConnectionFailureDescription> onStoreDisconnected)
        {
            m_StoreConnectUseCase.OnStoreDisconnection -= onStoreDisconnected;
        }
    }
}
