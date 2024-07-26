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
        public IAppleStoreExtendedService? Apple { get; }

        /// <summary>
        /// Google Specific Store Extensions
        /// </summary>
        public IGooglePlayStoreExtendedService? Google { get; }

        /// <summary>
        /// Amazon Specific Store Extensions
        /// </summary>
        public IAmazonAppsStoreExtendedService? Amazon { get; }

        /// <summary>
        /// Initiates a connection to the store.
        /// </summary>
        /// <returns>Return a handle to the initialization operation.</returns>
        /// <exception cref="StoreConnectionException">Throws an exception if the connection fails.</exception>
        Task ConnectAsync();

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
        /// Add an action to be called when connection is lost to the current store.
        /// </summary>
        /// <param name="onStoreDisconnected">The action to be added to the list of callbacks.</param>
        void AddOnStoreDisconnectedAction(Action<StoreConnectionFailureDescription> onStoreDisconnected);

        /// <summary>
        /// Remove an action to be called when connection is lost to the current store.
        /// </summary>
        /// <param name="onStoreDisconnected">The action to be removed from the list of callbacks.</param>
        void RemoveOnStoreDisconnectedAction(Action<StoreConnectionFailureDescription> onStoreDisconnected);
    }
}
