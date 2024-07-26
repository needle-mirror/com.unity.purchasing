using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Purchasing.Extension
{
    abstract class InternalStore : Store, IInternalStore
    {
        internal IStoreConnectionStateService StoreConnectionStateService;

        protected InternalStore()
        {
            StoreConnectionStateService = new StoreConnectionStateService();
        }

        public ConnectionState GetStoreConnectionState()
        {
            return StoreConnectionStateService.GetConnectionState();
        }

        public void SetStoreConnectionState(ConnectionState connectionState)
        {
            StoreConnectionStateService.SetConnectionState(connectionState);
        }
    }
}
