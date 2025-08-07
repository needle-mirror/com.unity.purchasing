using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;
namespace UnityEngine.Purchasing
{
    class GooglePlayStoreConnectionUseCase : IGooglePlayStoreConnectionUseCase
    {
        readonly IGoogleBillingClient m_BillingClient;

        [Preserve]
        public GooglePlayStoreConnectionUseCase(IGoogleBillingClient billingClient)
        {
            m_BillingClient = billingClient;
        }

        public void EndConnection()
        {
            m_BillingClient.EndConnection();
        }
    }
}
