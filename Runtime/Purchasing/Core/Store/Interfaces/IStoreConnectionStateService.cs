namespace UnityEngine.Purchasing
{
    interface IStoreConnectionStateService
    {
        /// <summary>
        /// Get the store connection state
        /// </summary>
        ConnectionState GetConnectionState();

        /// <summary>
        /// Sets the store connection state
        /// </summary>
        /// <param name="connectionState">store connection state</param>
        void SetConnectionState(ConnectionState connectionState);
    }
}
