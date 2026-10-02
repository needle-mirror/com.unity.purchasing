using System;
using Unity.Purchasing.Editor.Shared.Logging;

namespace UnityEditor.Purchasing.Editor.Authoring.RoutingAdminApi
{
    static class RoutingAdminEnvironment
    {
        const string k_CloudEnvironmentArg = "-cloudEnvironment";
        const string k_StagingEnvironment = "staging";

        const string k_ProductionBasePath = "https://services.unity.com/api/iap/v1";
        const string k_StagingBasePath = "https://staging.services.unity.com/api/iap/v1";

        internal static string BasePath => GetBasePath(Environment.GetCommandLineArgs());

        internal static string GetBasePath(string[] commandLineArgs)
        {
            return IsStagingEnvironment(commandLineArgs) ? k_StagingBasePath : k_ProductionBasePath;
        }

        static bool IsStagingEnvironment(string[] commandLineArgs)
        {
            try
            {
                var index = Array.IndexOf(commandLineArgs, k_CloudEnvironmentArg);
                if (index >= 0 && index <= commandLineArgs.Length - 2)
                {
                    return string.Equals(
                        commandLineArgs[index + 1],
                        k_StagingEnvironment,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception e)
            {
                Logger.LogVerbose(e);
            }

            return false;
        }
    }
}
