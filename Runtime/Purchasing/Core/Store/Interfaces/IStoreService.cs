#nullable enable
using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A service responsible for connecting to a store and its extensions.
    /// </summary>
    public interface IStoreService
    {
        /// <summary>
        /// Apple Specific Store Extensions
        /// </summary>
        IAppleStoreExtendedService? Apple { get; }

        /// <summary>
        /// Google Specific Store Extensions
        /// </summary>
        IGooglePlayStoreExtendedService? Google { get; }

        /// <summary>
        /// Initiates a connection to the store.
        /// </summary>
        /// <returns>Return a handle to the initialization operation.</returns>
        Task Connect();

        //TODO https://jira.unity3d.com/browse/IAP-3119
        // /// <summary>
        // /// Get the connection state to a given store.
        // /// </summary>
        // /// <returns>The state of the connection to the store.</returns>
        // ConnectionState GetConnectionState();

        /// <summary>
        /// Set a custom reconnection policy when the store disconnects.
        /// </summary>
        /// <param name="retryPolicy">The policy that will be used to determine if reconnection should be attempted.</param>
        void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy);

        /// <summary>
        /// Callback when connection is lost to the current store.
        /// </summary>
        event Action<StoreConnectionFailureDescription>? OnStoreDisconnected;
    }
}
