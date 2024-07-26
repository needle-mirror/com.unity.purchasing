#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    interface IStoreConnectUseCase
    {
        Task Connect();
        void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy retryPolicy);
        event Action<StoreConnectionFailureDescription>? OnStoreDisconnection;
    }
}
