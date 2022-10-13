using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
    class LegacyAnalyticsWrapper : IAnalyticsAdapter, ICoreServicesEnvironmentObserver
    {
        bool m_Enabled = true;
        IAnalyticsAdapter m_LegacyAdapter;
        IAnalyticsAdapter m_EmptyAdapter;

        internal LegacyAnalyticsWrapper(IAnalyticsAdapter legacyAdapter, IAnalyticsAdapter emptyAdapter)
        {
            m_LegacyAdapter = legacyAdapter;
            m_EmptyAdapter = emptyAdapter;
            CoreServicesEnvironmentSubject.Instance().SubscribeToUpdatesAndGetCurrent(this);
        }

        public void SendTransactionEvent(Product product)
        {
            m_AnalyticsAdapter.SendTransactionEvent(product);
        }

        public void SendTransactionFailedEvent(Product product, PurchaseFailureReason reason)
        {
            m_AnalyticsAdapter.SendTransactionFailedEvent(product, reason);
        }

        public void OnUpdatedCoreServicesEnvironment(string currentEnvironment)
        {
            m_Enabled = CoreServicesEnvironmentSubject.Instance().IsDefaultLiveEnvironment(currentEnvironment);
        }

        IAnalyticsAdapter m_AnalyticsAdapter { get { return m_Enabled ? m_LegacyAdapter : m_EmptyAdapter; } }
    }
}
