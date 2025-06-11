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
            try
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (OnStoreDisconnection == null)
                {
                    Debug.unityLogger.LogIAPWarning("IStoreService.Connect called without a callback defined for IStoreService.OnStoreDisconnected.");
                }
#endif

                (m_Store as IInternalStore)?.SetStoreConnectionState(ConnectionState.Connecting);
                if (m_CurrentConnectionCompletion == null || m_CurrentConnectionCompletion.Task.IsCompleted)
                {
                    m_CurrentConnectionCompletion = new TaskCompletionSource<object?>();
                    InitializeRequest();
                }

                return m_CurrentConnectionCompletion.Task;
            }
            catch (Exception e)
            {
                OnStoreDisconnection?.Invoke(new StoreConnectionFailureDescription(e.Message));
                return Task.CompletedTask;
            }
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
            OnStoreDisconnection?.Invoke(connectionFailureDescription);
            m_CurrentConnectionCompletion?.TrySetResult(null);

        }

        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy retryPolicy)
        {
            m_RetryPolicyOnDisconnect = retryPolicy;
        }
    }
}
