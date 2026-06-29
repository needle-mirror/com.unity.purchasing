using System;
using Unity.Purchasing.Editor.Shared.Logging;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    /// <summary>
    /// Resolves the Live Content Admin API base URL based on the cloud environment.
    /// Uses the same <c>-cloudEnvironment staging</c> command-line convention as
    /// <c>AuthenticationAdminClientManager</c> so that a single flag switches all
    /// services to their staging endpoints.
    /// </summary>
    static class LiveContentAdminEnvironment
    {
        const string k_CloudEnvironmentArg = "-cloudEnvironment";
        const string k_StagingEnvironment = "staging";

        const string k_ProductionBasePath = "https://services.api.unity.com/live-content/admin";
        const string k_StagingBasePath = "https://staging.services.api.unity.com/live-content/admin";

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
