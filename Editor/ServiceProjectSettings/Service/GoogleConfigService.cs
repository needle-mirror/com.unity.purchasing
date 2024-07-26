namespace UnityEditor.Purchasing
{
    internal class GoogleConfigService
    {
        static GoogleConfigService m_Instance;

        internal GoogleConfigurationData GoogleConfigData { get; }

        GoogleConfigService()
        {
            GoogleConfigData = new GoogleConfigurationData();
        }

        internal static GoogleConfigService Instance()
        {
            m_Instance ??= new GoogleConfigService();

            return m_Instance;
        }
    }
}
