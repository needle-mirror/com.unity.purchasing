#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.PaymentProviderService.Http;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Resolves a native (Apple/Google) store-specific id to its Unity uSku (+ ProductType) via
    /// the backend store-overrides endpoint. Used only when the local catalog has no matching
    /// entry, so callers can then <see cref="IProductCache.Find"/> the real cached product by that
    /// uSku — or, on cache miss, synthesize an Unknown Product carrying the resolved type.
    ///
    /// The full override map for the store is fetched once per session and cached; individual
    /// native ids are resolved locally against it. A failed fetch is evicted so a later call
    /// retries; HTTP 404 (project has no remote catalog) caches an empty map so the endpoint is
    /// never hit again this session.
    ///
    /// Wired only on Apple and Google stores; PaymentProvider/Fake/etc. deliberately skip
    /// this. The service self-constructs its API client lazily from <c>CoreRegistry</c>, so
    /// it works whether or not the PaymentProvider store was ever instantiated.
    /// </summary>
    internal class StoreOverrideReverseLookupService : IStoreOverrideReverseLookupService
    {
        readonly Func<IStoreOverrideLookupClient> m_ClientFactory;
        readonly string m_Store;
        readonly object m_Lock = new();
        Task<Dictionary<string, StoreOverrideMatch>>? m_FetchTask;
        IStoreOverrideLookupClient? m_Client;

        public StoreOverrideReverseLookupService(string store)
            : this(store, () => new StoreOverrideLookupClient())
        {
        }

        internal StoreOverrideReverseLookupService(string store, Func<IStoreOverrideLookupClient> clientFactory)
        {
            m_Store = store;
            m_ClientFactory = clientFactory;
        }

        public async Task<ResolvedUSku?> ResolveAsync(string? nativeId)
        {
            if (string.IsNullOrEmpty(nativeId))
            {
                return null;
            }

            Task<Dictionary<string, StoreOverrideMatch>> task;
            lock (m_Lock)
            {
                task = m_FetchTask ??= FetchAll();
            }

            Dictionary<string, StoreOverrideMatch> map;
            try
            {
                map = await task;
            }
            catch (Exception e)
            {
                // Evict only *our* faulted task so a concurrent caller's fresh in-flight task isn't dropped.
                lock (m_Lock)
                {
                    if (ReferenceEquals(m_FetchTask, task))
                    {
                        m_FetchTask = null;
                    }
                }
                Debug.unityLogger.LogIAPWarning($"Store override reverse-lookup failed for '{nativeId}': {e.Message}");
                return null;
            }

            if (!map.TryGetValue(nativeId!, out var match) || string.IsNullOrEmpty(match?.USku))
            {
                return null;
            }
            return new ResolvedUSku(match.USku, MapProductType(match.Type));
        }

        async Task<Dictionary<string, StoreOverrideMatch>> FetchAll()
        {
            m_Client ??= m_ClientFactory();
            try
            {
                return await m_Client.Lookup(m_Store);
            }
            catch (HttpException e) when (e.Response?.StatusCode == 404)
            {
                // Project has no remote catalog configured; cache the empty map so all future
                // lookups short-circuit without hitting the endpoint again.
                return new Dictionary<string, StoreOverrideMatch>();
            }
        }

        static ProductType MapProductType(string? backendType)
        {
            return InternalPaymentProviderService.CatalogProductTypeFromString(backendType) switch
            {
                CatalogProductType.Consumable => ProductType.Consumable,
                CatalogProductType.NonConsumable => ProductType.NonConsumable,
                CatalogProductType.Subscription => ProductType.Subscription,
                _ => ProductType.Unknown,
            };
        }
    }
}
