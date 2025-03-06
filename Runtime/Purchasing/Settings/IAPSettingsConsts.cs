namespace UnityEngine.Purchasing
{
    internal static class IapSettingsConsts
    {
        internal const string StagingPath = "https://staging.services.unity.com";
        internal const string ProductionPath = "https://services.unity.com";
        internal const string ApiPath = "/api/iap-settings/v1";
        internal const string SettingsEndpoint = "/projects/{0}/settings";

        internal const string AuthorizationHeaderValue = "Bearer {0}";
        internal const string XClientIdHeader = "unity-dashboard";
    }
}
