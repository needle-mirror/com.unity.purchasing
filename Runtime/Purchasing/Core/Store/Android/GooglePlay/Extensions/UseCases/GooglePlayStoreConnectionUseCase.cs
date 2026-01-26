using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreConnectionUseCase : IGooglePlayStoreConnectionUseCase
    {
        readonly IBillingClient m_BillingClient;

        [Preserve]
        public GooglePlayStoreConnectionUseCase(IBillingClient billingClient)
        {
            m_BillingClient = billingClient;
        }

        public void EndConnection()
        {
            m_BillingClient.EndConnection();
        }
    }
}
