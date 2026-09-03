using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Purchasing.Editor.Shared.Clients;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    static class LiveContentAdminApiHeaderConfigurator
    {
        internal static async Task UpdateAuthenticationHeaders<T>(
            IConfigsApi configsApi,
            Func<Task<string>> getToken)
        {
            var configuration = configsApi?.Configuration;
            if (configuration == null)
                return;

            var token = await getToken();

            // Use copy-on-write so a token refresh cannot mutate the collection while a request
            // is enumerating it, and preserve headers configured when the API is registered.
            var mergedHeaders = configuration.DefaultHeaders == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(configuration.DefaultHeaders);

            foreach (var header in new AdminApiHeaders<T>(token).ToDictionary())
                mergedHeaders[header.Key] = header.Value;

            configuration.DefaultHeaders = mergedHeaders;
        }
    }
}
