#nullable enable

namespace UnityEngine.Purchasing
{
    class StoreConnectionStateService : IStoreConnectionStateService
    {
        ConnectionState? m_StoreConnectionState;

        public StoreConnectionStateService()
        {
            m_StoreConnectionState = ConnectionState.Unavailable;
        }

        public ConnectionState GetConnectionState()
        {
            return m_StoreConnectionState ?? ConnectionState.Unavailable;
        }

        public void SetConnectionState(ConnectionState connectionState)
        {
            m_StoreConnectionState = connectionState;
        }
    }
}
