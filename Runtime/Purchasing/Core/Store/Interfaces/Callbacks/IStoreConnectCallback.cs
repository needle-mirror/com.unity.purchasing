#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface to handle callbacks for managing the connection to a store.
    /// </summary>
    public interface IStoreConnectCallback
    {
        /// <summary>
        /// Callback received when the connection is successfully initialized.
        /// </summary>
        void OnStoreConnectionSucceeded();

        /// <summary>
        /// Callback received when the connection to the store fails.
        /// </summary>
        /// <param name="failureDescription">The description of the failure.</param>
        void OnStoreConnectionFailed(StoreConnectionFailureDescription failureDescription);
    }
}
