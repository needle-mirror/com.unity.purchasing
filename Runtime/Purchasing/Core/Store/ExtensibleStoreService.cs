#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// An abstract store service to extend an existing Store Service which will handle all of the basic IStoreService implementations
    /// The main purpose of this is to allow a custom store to add implementations of extended features to this service.
    /// The calls to IStoreService are kept virtual so that the derivations of the base store implementing them can be added to or overridden.
    /// </summary>
    public abstract class ExtensibleStoreService : IStoreService
    {
        IStoreService m_BaseInternalStoreService;

        /// <summary>
        /// Constructor to be used by derived classes
        /// </summary>
        /// <param name="baseStoreService"> The base service implementation which implements IStoreService </param>
        protected ExtensibleStoreService(IStoreService baseStoreService)
        {
            m_BaseInternalStoreService = baseStoreService;
        }

        /// <summary>
        /// Apple Specific Store Extensions
        /// </summary>
        public virtual IAppleStoreExtendedService? Apple => m_BaseInternalStoreService.Apple;

        /// <summary>
        /// Google Specific Store Extensions
        /// </summary>
        public virtual IGooglePlayStoreExtendedService? Google => m_BaseInternalStoreService.Google;

        /// <summary>
        /// Initiates a connection to the store.
        /// </summary>
        /// <returns>Return a handle to the initialization operation.</returns>
        public virtual Task Connect()
        {
            return m_BaseInternalStoreService.Connect();
        }

        /// <summary>
        /// Set a custom reconnection policy when the store disconnects.
        /// </summary>
        /// <param name="retryPolicy">The policy that will be used to determine if reconnection should be attempted.</param>
        public virtual void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy)
        {
            m_BaseInternalStoreService.SetStoreReconnectionRetryPolicyOnDisconnection(retryPolicy);
        }

        /// <summary>
        /// Callback for when the store disconnects.
        /// </summary>
        public event Action<StoreConnectionFailureDescription>? OnStoreDisconnected
        {
            add => m_BaseInternalStoreService.OnStoreDisconnected += value;
            remove => m_BaseInternalStoreService.OnStoreDisconnected -= value;
        }
    }
}
