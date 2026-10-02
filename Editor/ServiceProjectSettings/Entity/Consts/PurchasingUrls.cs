using System;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    static class PurchasingUrls
    {
        // Same launch argument Core reads (CloudEnvironmentConfigProvider, internal to Core), so requests go to the
        // environment that issued the gateway token, and links open that environment's dashboard.
        static readonly bool k_IsStaging = IsStagingCloudEnvironment(Environment.GetCommandLineArgs());

        internal static string ServicesHost => k_IsStaging ? IapSettingsConsts.StagingPath : IapSettingsConsts.ProductionPath;
        static string DashboardHost => k_IsStaging ? "https://staging.cloud.unity.com" : "https://cloud.unity.com";

        internal static string iapSettingssUrl => ServicesHost + IapSettingsConsts.ApiPath + IapSettingsConsts.SettingsEndpoint;

        internal static string cloudProjectSettingsUrl => DashboardHost + "/home/organizations/{0}/projects/{1}/settings";

        internal static string paymentProviderUrl => DashboardHost + "/home/organizations/{0}/projects/{1}/environments/{2}/in-app-purchase/payment-providers";
        internal static string inAppPurchasesUrl => DashboardHost + "/home/organizations/{0}/in-app-purchase/about";

        // Accepts both "-cloudEnvironment staging" and "-cloudEnvironment=staging", like Core.
        internal static bool IsStagingCloudEnvironment(string[] args)
        {
            const string flag = "-cloudEnvironment";
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == flag && i + 1 < args.Length)
                {
                    return args[i + 1] == "staging";
                }

                if (args[i].StartsWith(flag + "="))
                {
                    return args[i].Substring(flag.Length + 1) == "staging";
                }
            }

            return false;
        }
    }
}
