namespace UnityEngine.Purchasing.Extension
{
    interface IInternalStore : IStore
    {
        /// <summary>
        /// Gets the store connection state
        /// </summary>
        /// <returns>The store connection state</returns>
        ConnectionState GetStoreConnectionState();

        /// <summary>
        /// Sets the store connection state
        /// </summary>
        /// <param name="connectionState">store connection state</param>
        void SetStoreConnectionState(ConnectionState connectionState);
    }
}
