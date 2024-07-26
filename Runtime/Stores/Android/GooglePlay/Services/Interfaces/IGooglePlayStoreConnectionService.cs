using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreConnectionService
    {
        void Connect();

        bool IsReady();

        GoogleBillingConnectionState CheckConnectionState();

        void SetConnectionCallback(IStoreConnectCallback storeConnectCallback);
    }
}
