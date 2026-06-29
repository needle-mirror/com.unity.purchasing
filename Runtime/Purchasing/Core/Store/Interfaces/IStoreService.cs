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
        /// Payment Provider Specific Store Extensions
        /// </summary>
        IPaymentProvidersExtendedService? PaymentProviders { get; }

        /// <summary>
        /// Initiates a connection to the store.
        /// </summary>
        /// <returns>Return a handle to the initialization operation.</returns>
        Task Connect();

        /// <summary>
        /// Get the connection state to a given store.
        /// </summary>
        /// <returns>The state of the connection to the store.</returns>
        ConnectionState GetConnectionState();

        /// <summary>
        /// Set a custom reconnection policy when the store disconnects.
        /// </summary>
        /// <param name="retryPolicy">The policy that will be used to determine if reconnection should be attempted.</param>
        void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy);

        /// <summary>
        /// Callback when connection is lost to the current store.
        /// </summary>
        event Action<StoreConnectionFailureDescription>? OnStoreDisconnected;

        /// <summary>
        /// Callback when connection to the store is successfully established.
        /// </summary>
        event Action? OnStoreConnected;

        /// <summary>
        /// Raised after the authenticated end-user account changes; this store's product
        /// and purchase caches are cleared before the event fires. With no subscriber
        /// attached, caches are not cleared on account change. From your handler, re-run
        /// your init flow for the new account: FetchCatalog → FetchProducts → FetchPurchases.
        /// <para>
        /// Requires <c>com.unity.services.authentication</c> 3.0.0 or later. Each active store
        /// fires its own event.
        /// </para>
        /// </summary>
        event Action? OnAuthAccountChanged;
    }
}

