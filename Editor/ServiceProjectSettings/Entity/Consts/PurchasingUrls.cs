using System;
using System.Linq;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.OrganizationHandler;
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
        // {0} project, {1} environment.
        internal static string paymentProviderConfigurationUrl => ServicesHost + "/api/iap/v1/projects/{0}/environments/{1}/payment-provider/configuration";

        internal static string cloudProjectSettingsUrl => DashboardHost + "/home/organizations/{0}/projects/{1}/settings";

        internal static string paymentProviderUrl => DashboardHost + "/home/organizations/{0}/projects/{1}/environments/{2}/in-app-purchase/payment-providers";
        internal static string remoteCatalogUrl => DashboardHost + "/home/organizations/{0}/projects/{1}/environments/{2}/in-app-purchase/catalog";
        internal static string inAppPurchasesUrl => DashboardHost + "/home/organizations/{0}/in-app-purchase/about";

        // Formats an environment-scoped page ({0} org, {1} project, {2} environment) for the active environment, falling back to
        // production, and to the organization's In-App Purchasing page when the environments aren't known yet.
        internal static string BuildEnvironmentPageUrl(string environmentPageUrlFormat)
        {
            var environmentId = ResolveEnvironmentId();
            var organizationKey = OrganizationProvider.Organization.Key;
            return environmentId == Guid.Empty
                ? string.Format(inAppPurchasesUrl, organizationKey)
                : string.Format(environmentPageUrlFormat, organizationKey, CloudProjectSettings.projectId, environmentId);
        }

        // The active environment, else production, else Guid.Empty while the environments aren't known yet.
        internal static Guid ResolveEnvironmentId()
        {
            var environmentId = EnvironmentsApi.Instance.ActiveEnvironmentId ?? Guid.Empty;
            if (environmentId == Guid.Empty)
            {
                environmentId = EnvironmentsApi.Instance.Environments?.FirstOrDefault(environment => environment.Name == "production").Id ?? Guid.Empty;
            }

            return environmentId;
        }

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
