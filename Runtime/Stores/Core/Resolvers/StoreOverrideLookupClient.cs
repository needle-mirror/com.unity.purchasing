#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Internal;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.PaymentProviderService.Apis.PaymentProvider;
using UnityEngine.Purchasing.PaymentProviderService.Http;
using UnityEngine.Purchasing.PaymentProviderService.Models;
using UnityEngine.Purchasing.PaymentProviderService.PaymentProvider;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Production <see cref="IStoreOverrideLookupClient"/> that lazily constructs its own
    /// <see cref="PaymentProviderApiClient"/> on first use, pulling auth + project from
    /// <c>CoreRegistry</c>. Independent of <c>PaymentProviderServiceProvider</c> so the
    /// resolver works on Apple/Google stores even when the PaymentProvider store was never
    /// instantiated.
    /// </summary>
    internal class StoreOverrideLookupClient : IStoreOverrideLookupClient
    {
        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";
        const string k_StagingEnvironment = "staging";

        PaymentProviderApiClient? m_ApiClient;
        Configuration? m_Configuration;
        string? m_ProjectId;

        public async Task<Dictionary<string, StoreOverrideMatch>> Lookup(string store)
        {
            if (!TryEnsureClient())
            {
                throw new InvalidOperationException("Unity Services not initialised; cannot resolve store overrides.");
            }

            var request = new LookupStoreOverridesRequest(m_ProjectId!, store);
            var response = await m_ApiClient!.LookupStoreOverridesAsync(request, m_Configuration);
            var byStore = response.Result;
            if (byStore == null)
            {
                return new Dictionary<string, StoreOverrideMatch>();
            }
            var entry = byStore.FirstOrDefault(kv => string.Equals(kv.Key, store, StringComparison.OrdinalIgnoreCase));
            if (entry.Value == null)
            {
                return new Dictionary<string, StoreOverrideMatch>();
            }
            return entry.Value
                .Where(kv => !string.IsNullOrEmpty(kv.Value?.USku))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        bool TryEnsureClient()
        {
            if (m_ApiClient != null)
            {
                return true;
            }

            try
            {
                var registry = CoreRegistry.Instance;
                var accessToken = registry.GetServiceComponent<IAccessToken>();
                var cloudProjectId = registry.GetServiceComponent<ICloudProjectId>().GetCloudProjectId();
                if (string.IsNullOrEmpty(accessToken?.AccessToken) || string.IsNullOrEmpty(cloudProjectId))
                {
                    return false;
                }

                string host;
                try
                {
                    var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();
                    host = ResolveHost(projectConfiguration);
                }
                catch
                {
                    host = "https://iap.services.api.unity.com";
                }

                m_ApiClient = new PaymentProviderApiClient(new HttpClient(), accessToken);
                m_Configuration = new Configuration(host, 10, 4, null);
                m_Configuration.Headers.Add("Unity-IAP-Package-Version", IAPVersion.Current);
                m_ProjectId = cloudProjectId;
                return true;
            }
            catch
            {
                // Required component(s) missing; caller will see a retry on a later attempt.
                m_ApiClient = null;
                m_Configuration = null;
                m_ProjectId = null;
                return false;
            }
        }

        static string ResolveHost(IProjectConfiguration cfg)
        {
            return cfg?.GetString(k_CloudEnvironmentKey) == k_StagingEnvironment
                ? "https://iap-stg.services.api.unity.com"
                : "https://iap.services.api.unity.com";
        }
    }
}
