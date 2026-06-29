// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Linq;

namespace Unity.Purchasing.Editor.Shared.Clients
{
    static class CloudEnvironmentConfigProvider
    {
        const string k_CloudEnvironmentArg = "-cloudEnvironment";
        public const string StagingEnvironment = "staging";

        public static string GetCloudEnvironment()
        {
            return GetCloudEnvironment(Environment.GetCommandLineArgs());
        }

        public static bool IsStaging()
        {
            return GetCloudEnvironment() == StagingEnvironment;
        }

        static string GetCloudEnvironment(string[] commandLineArgs)
        {
            var cloudEnvironmentField = commandLineArgs.FirstOrDefault(x => x.StartsWith(k_CloudEnvironmentArg));

            if (cloudEnvironmentField != null)
            {
                var cloudEnvironmentIndex = Array.IndexOf(commandLineArgs, cloudEnvironmentField);

                if (cloudEnvironmentField == k_CloudEnvironmentArg)
                {
                    if (cloudEnvironmentIndex <= commandLineArgs.Length - 2)
                    {
                        return commandLineArgs[cloudEnvironmentIndex + 1];
                    }
                }
                else if (cloudEnvironmentField.Contains('='))
                {
                    var value = cloudEnvironmentField.Substring(cloudEnvironmentField.IndexOf('=') + 1);
                    return !string.IsNullOrEmpty(value) ? value : null;
                }
            }

            return null;
        }
    }
}
