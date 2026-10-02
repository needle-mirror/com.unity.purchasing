namespace UnityEditor.Purchasing
{
    internal class GoogleConfigService
    {
        static GoogleConfigService m_Instance;

        readonly GoogleConfigurationData m_GoogleConfigData;

        internal GoogleConfigurationData GoogleConfigData
        {
            get
            {
                m_GoogleConfigData.ResetIfCloudProjectChanged();
                return m_GoogleConfigData;
            }
        }

        GoogleConfigService()
        {
            m_GoogleConfigData = new GoogleConfigurationData();
        }

        internal static GoogleConfigService Instance()
        {
            m_Instance ??= new GoogleConfigService();

            return m_Instance;
        }
    }
}
