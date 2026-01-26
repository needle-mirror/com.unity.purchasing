using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing.GoogleBilling.Interfaces
{
    interface IBillingClientBase
    {
        void StartConnection(IBillingClientStateListener billingClientStateListener);
        void EndConnection();
        bool IsReady();
        GoogleBillingConnectionState GetConnectionState();
    }
}
