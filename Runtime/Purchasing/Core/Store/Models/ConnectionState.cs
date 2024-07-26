namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The connection state to a store.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// Disconnected from store service.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Connecting to store service.
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected to store service.
        /// </summary>
        Connected,

        /// <summary>
        /// Disconnecting from store service.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Store Service Unavailable
        /// </summary>
        Unavailable
    }
}
