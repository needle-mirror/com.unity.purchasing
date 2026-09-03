#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uniject;
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.Stores;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.CatalogListings
{
    internal class CatalogListingClient : ICatalogListingClient
    {
        const string k_ProductionSchemaUrl =
            "https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog";

        const string k_StagingSchemaUrl =
            "https://staging.services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog";

        const string k_StagingEnvironment = "staging";

        internal const string k_WebshopSchemaPath = "/v1/schemas/UnityRemoteCatalogWebshop";

        const string k_SchemaVersion = ">=1.1.0,<2.0.0";

        const int k_Limit = 100;
        const int k_MaxAttemptsPerPage = 4;
        static readonly float[] k_BackoffSecondsPerAttempt = { 0.2f, 0.4f, 0.8f };

        readonly ILiveContentAdapterClientWrapper m_LiveContentAdapterClientWrapper;
        readonly ICatalogListingParser m_Parser;
        readonly IUtil m_Util;
        readonly Func<string?> m_CloudEnvironmentProvider;

        public CatalogListingClient(
            ILiveContentAdapterClientWrapper liveContentAdapterClientWrapper,
            ICatalogListingParser parser,
            IUtil util,
            Func<string?> cloudEnvironmentProvider)
        {
            m_LiveContentAdapterClientWrapper = liveContentAdapterClientWrapper;
            m_Parser = parser;
            m_Util = util;
            m_CloudEnvironmentProvider = cloudEnvironmentProvider;
        }

        public bool IsAvailable => m_LiveContentAdapterClientWrapper.LiveContentAdapterClientIsAvailable;

        public async Task<CatalogListingResult> GetCatalogListings()
        {
            var schemaUrlEncoded = Uri.EscapeDataString(ResolveSchemaUrl());
            var liveContentService = m_LiveContentAdapterClientWrapper.GetLiveContentAdapterService();

            var accumulated = new List<CatalogListingDto>();
            string? cursor = null;

            while (true)
            {
                List<ConfigContentData>? pageConfigs = null;

                for (var attempt = 0; attempt < k_MaxAttemptsPerPage; attempt++)
                {
                    try
                    {
                        pageConfigs = await liveContentService.GetConfigsContent(
                            schema: schemaUrlEncoded,
                            schemaVersion: k_SchemaVersion,
                            limit: k_Limit,
                            after: cursor);
                        break;
                    }
                    // Only transient errors are retried. Non-retriable failures (auth, validation, etc.)
                    // propagate so callers can surface a meaningful failure reason instead of a generic
                    // "paging incomplete" partial result.
                    catch (LiveContentAdapterException ex) when (ex.IsRetriable())
                    {
                        if (attempt + 1 >= k_MaxAttemptsPerPage)
                        {
                            return new CatalogListingResult
                            {
                                Results = accumulated,
                                CompletedSuccessfully = false,
                                LastFailedAfter = cursor
                            };
                        }
                        await WaitForSecondsAsync(k_BackoffSecondsPerAttempt[attempt]);
                    }
                }

                accumulated.AddRange(m_Parser.TryParseCatalogResponse(pageConfigs!));

                if (pageConfigs!.Count < k_Limit)
                {
                    return new CatalogListingResult
                    {
                        Results = accumulated,
                        CompletedSuccessfully = true
                    };
                }

                cursor = pageConfigs.LastOrDefault()?.path;
            }
        }

        internal string ResolveSchemaUrl()
        {
            return m_CloudEnvironmentProvider() == k_StagingEnvironment
                ? k_StagingSchemaUrl
                : k_ProductionSchemaUrl;
        }

        // WebGL-safe delay: coroutine-driven via IUtil + TaskCompletionSource. Avoids Task.Delay which
        // requires threading support that WebGL doesn't have.
        Task WaitForSecondsAsync(float seconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            m_Util.InitiateCoroutine(WaitCoroutine(seconds, tcs));
            return tcs.Task;
        }

        static IEnumerator WaitCoroutine(float seconds, TaskCompletionSource<bool> tcs)
        {
            yield return new WaitForSecondsRealtime(seconds);
            tcs.SetResult(true);
        }
    }
}
