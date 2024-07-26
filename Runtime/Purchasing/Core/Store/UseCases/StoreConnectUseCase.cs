#nullable enable

using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class StoreConnectUseCase : IStoreConnectUseCase, IStoreConnectCallback
    {
        readonly IStore m_Store;
        readonly IRetryService m_RetryService;
        IRetryPolicy m_RetryPolicyOnDisconnect;
        public event Action<StoreConnectionFailureDescription>? OnStoreDisconnection;

        TaskCompletionSource<object?>? m_CurrentConnectionCompletion;
        IRetryRequest? m_CurrentRequest;

        internal StoreConnectUseCase(IStore store, IRetryService retryService, IRetryPolicy retryPolicyOnDisconnect)
        {
            m_RetryService = retryService;
            m_Store = store;
            m_Store.SetStoreConnectionCallback(this);
            m_RetryPolicyOnDisconnect = retryPolicyOnDisconnect;
        }

        public Task Connect()
        {
            (m_Store as IInternalStore)?.SetStoreConnectionState(ConnectionState.Connecting);
            if (m_CurrentConnectionCompletion == null || m_CurrentConnectionCompletion.Task.IsCompleted)
            {
                m_CurrentConnectionCompletion = new TaskCompletionSource<object?>();
                InitializeRequest();
            }

            return m_CurrentConnectionCompletion.Task;
        }

        void InitializeRequest()
        {
            m_CurrentRequest = m_RetryService.CreateRequest(m_Store.Connect, m_RetryPolicyOnDisconnect);
            m_CurrentRequest.Invoke();
        }

        public void OnStoreConnectionSucceeded()
        {
            (m_Store as IInternalStore)?.SetStoreConnectionState(ConnectionState.Connected);
            m_CurrentConnectionCompletion?.TrySetResult(null);
            m_CurrentRequest = null;
        }

        public async void OnStoreConnectionFailed(StoreConnectionFailureDescription failureDescription)
        {
            m_CurrentRequest ??= m_RetryService.CreateRequest(m_Store.Connect, m_RetryPolicyOnDisconnect);

            var hasRetried = await m_CurrentRequest.Retry(failureDescription);
            if (!hasRetried)
            {
                SendDisconnectionEvent(failureDescription);
            }
        }

        void SendDisconnectionEvent(StoreConnectionFailureDescription connectionFailureDescription)
        {
            (m_Store as IInternalStore)?.SetStoreConnectionState(ConnectionState.Disconnected);
            var exception = new StoreConnectionException(connectionFailureDescription);
            m_CurrentConnectionCompletion?.TrySetException(exception);

            OnStoreDisconnection?.Invoke(connectionFailureDescription);
        }

        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy retryPolicy)
        {
            m_RetryPolicyOnDisconnect = retryPolicy;
        }
    }
}
