#nullable enable
using System;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Internal;
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.CatalogListings
{
    internal static class CatalogListingClientProvider
    {
        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";

        static ICatalogListingClient? s_Instance;

        public static ICatalogListingClient Instance()
        {
            return s_Instance ??= new CatalogListingClient(
                LiveContentAdapterServiceProvider.Instance(),
                new CatalogListingParser(),
                UnityUtilContainer.Instance(),
                GetCloudEnvironment
            );
        }

        static string? GetCloudEnvironment()
        {
            try
            {
                return CoreRegistry.Instance
                    .GetServiceComponent<IProjectConfiguration>()
                    ?.GetString(k_CloudEnvironmentKey);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
