using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace UnityEditor.Purchasing.Editor.Authoring.Core
{
    internal class LiveContentConfigClient : ILiveContentConfigClient
    {
        static string GetCatalogListingPath(string catalogListingId)
        {
            var error = CatalogItem.GetCatalogListingIdValidationError(catalogListingId);
            if (error.HasValue)
                throw new ArgumentException(
                    $"{error.Value.Description}: {error.Value.Detail}",
                    nameof(catalogListingId));
            return catalogListingId;
        }

        const string k_SchemaRegistryBasePath =
            "https://services.api.unity.com/schema-registry";
        internal const string k_RequiredSchemaPath =
            "/v1/schemas/UnityRemoteCatalog/versions/1.1.0";
        internal const string k_WebshopSchemaPath =
            "/v1/schemas/UnityRemoteCatalogWebshop/versions/1.1.0";
        internal const string k_WebshopMarker = "UnityRemoteCatalogWebshop";

        // Partial-path match — survives prod/staging base-URL differences.
        internal static bool IsSdkControlled(string url) =>
            url != null && (url.Contains(k_RequiredSchemaPath) || url.Contains(k_WebshopMarker));

        const int k_MaxConcurrentFetches = 8;
        const int k_MaxFetchRetries = 4;
        static readonly Random s_Jitter = new Random();

        static readonly JsonSerializerSettings k_DtoSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        static readonly JsonSerializerSettings k_DeserSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        readonly ILogger m_Logger;
        readonly ILiveContentApiTransport m_Transport;
        readonly string m_RequiredSchema;
        readonly string m_WebshopSchema;

        public LiveContentConfigClient(ILiveContentApiTransport transport, ILogger logger)
            : this(transport, logger, k_SchemaRegistryBasePath) { }

        internal LiveContentConfigClient(
            ILiveContentApiTransport transport,
            ILogger logger,
            string schemaRegistryBasePath)
        {
            m_Transport = transport;
            m_Logger = logger;
            m_RequiredSchema = schemaRegistryBasePath + k_RequiredSchemaPath;
            m_WebshopSchema = schemaRegistryBasePath + k_WebshopSchemaPath;
        }

        public async Task Initialize(
            string environmentId,
            string projectId,
            CancellationToken cancellationToken)
        {
            await m_Transport.InitializeAsync(environmentId, projectId, cancellationToken);
        }

        public async Task<List<CatalogItem>> List(CancellationToken cancellationToken)
        {
            try
            {
                var configPaths = await FetchAllConfigPaths(cancellationToken);

                using var gate = new SemaphoreSlim(k_MaxConcurrentFetches);
                var fetchTasks = configPaths
                    .Select(configPath => FetchConfigWithRetry(configPath, gate, cancellationToken))
                    .ToList();

                var payloads = await Task.WhenAll(fetchTasks);

                var result = new List<CatalogItem>();
                for (var i = 0; i < payloads.Length; i++)
                {
                    var payload = payloads[i];
                    var configPath = configPaths[i];

                    if (!payload.IsSuccess)
                    {
                        if (payload.StatusCode == 404)
                            continue;

                        throw new ClientException(
                            $"Failed to fetch config at '{configPath}' " +
                            $"(HTTP {payload.StatusCode}). Aborting List() to avoid returning a partial catalog.",
                            null);
                    }

                    if (string.IsNullOrEmpty(payload.Content))
                        continue;

                    try
                    {
                        var dto = JsonConvert.DeserializeObject<CatalogItemDto>(payload.Content, k_DeserSettings);
                        if (dto == null)
                            continue;
                        if (string.IsNullOrEmpty(dto.uSku))
                        {
                            m_Logger.LogWarning($"Config at '{configPath}' has empty or missing uSku. Skipping.");
                            continue;
                        }
                        var item = dto.ToCatalogItem();
                        item.CatalogListingId = configPath;
                        result.Add(item);
                    }
                    catch (Exception e)
                    {
                        m_Logger.LogWarning($"Failed to deserialize config at '{configPath}'. Skipping. {e.Message}");
                    }
                }

                return result;
            }
            catch (ClientException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ClientException($"List() failed unexpectedly. {e.Message}", e);
            }
        }

        async Task<TransportResult> FetchConfigWithRetry(string configPath, SemaphoreSlim gate, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await SendWithRetry(
                    () => m_Transport.GetConfigContentAsync(configPath, cancellationToken),
                    cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        async Task<TransportResult> SendWithRetry(Func<Task<TransportResult>> sendRequest, CancellationToken cancellationToken)
        {
            TransportResult response = default;
            for (var attempt = 0; attempt < k_MaxFetchRetries; attempt++)
            {
                response = await sendRequest();

                if (response.IsSuccess || response.StatusCode == 404 || !IsTransientFailure(response))
                    return response;

                if (attempt < k_MaxFetchRetries - 1)
                {
                    var delay = ComputeRetryDelay(response, attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            return response;
        }

        static bool IsTransientFailure(TransportResult response)
        {
            return response.StatusCode == 0
                || response.StatusCode == 429
                || (response.StatusCode >= 500 && response.StatusCode < 600);
        }

        static TimeSpan ComputeRetryDelay(TransportResult response, int attempt)
        {
            var retryAfter = response.Headers?
                .FirstOrDefault(h => string.Equals(h.Key, "Retry-After", StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrEmpty(retryAfter)
                && int.TryParse(retryAfter, out var seconds)
                && seconds > 0)
            {
                return TimeSpan.FromSeconds(Math.Min(seconds, 30));
            }

            var backoffMs = Math.Min(1000 * (1 << attempt), 8000);
            int jitterMs;
            lock (s_Jitter) { jitterMs = s_Jitter.Next(0, 250); }
            return TimeSpan.FromMilliseconds(backoffMs + jitterMs);
        }

        async Task<List<string>> FetchAllConfigPaths(CancellationToken cancellationToken)
        {
            const int maxConfigsApiPageSize = 100;
            var configPaths = new List<string>();
            string afterCursor = null;
            var isFirstPage = true;
            List<string> allItems;

            do
            {
                var configsResponse = await SendWithRetry(
                    () => m_Transport.GetConfigPathsAsync(
                        pathPrefix: "catalog/",
                        limit: maxConfigsApiPageSize,
                        after: afterCursor,
                        start: isFirstPage ? true : (bool?)null,
                        schema: m_RequiredSchema,
                        noVariantTag: true,
                        cancellationToken: cancellationToken),
                    cancellationToken);

                isFirstPage = false;

                if (configsResponse.StatusCode == 404 || string.IsNullOrEmpty(configsResponse.Content))
                    break;

                if (!configsResponse.IsSuccess)
                    throw new ClientException(
                        $"GetConfigPaths failed (HTTP {configsResponse.StatusCode}). {configsResponse.Content}",
                        null);

                var json = JToken.Parse(configsResponse.Content);
                allItems = json.SelectTokens("$..path")
                    .Select(t => t.Value<string>())
                    .Where(p => p != null)
                    .ToList();

                configPaths.AddRange(allItems);
                afterCursor = allItems.LastOrDefault();

            } while (allItems.Count >= maxConfigsApiPageSize);

            return configPaths;
        }

        public async Task Upsert(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            try
            {
                await UpsertOneWithRetry(catalogItem, cancellationToken);
            }
            catch (ClientException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ClientException($"Upsert() failed unexpectedly. {e.Message}", e);
            }
        }

        async Task UpsertOneWithRetry(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            var path = GetCatalogListingPath(catalogItem.CatalogListingId);

            var exists = await SendWithRetry(
                () => m_Transport.GetConfigContentAsync(path, cancellationToken),
                cancellationToken);

            if (!exists.IsSuccess && exists.StatusCode != 404)
                throw new ClientException(
                    $"Failed to check existence of '{path}' (HTTP {exists.StatusCode}). {exists.Content}",
                    null);

            var dto = catalogItem.ToDto();
            dto.Schemas = BuildSchemas(catalogItem.IsWebshopAvailable);
            PreserveFromExisting(dto, exists);
            dto.ApplyManagedByMetadata();

            var jsonContent = JsonConvert.SerializeObject(dto, k_DtoSettings);

            TransportResult response;
            if (exists.IsSuccess && !string.IsNullOrEmpty(exists.Content))
            {
                response = await SendWithRetry(
                    () => m_Transport.UpdateConfigAsync(path, jsonContent, cancellationToken),
                    cancellationToken);
            }
            else
            {
                response = await SendWithRetry(
                    () => m_Transport.CreateConfigAsync(path, jsonContent, cancellationToken),
                    cancellationToken);
            }

            if (!response.IsSuccess)
                throw new ClientException(
                    $"Failed to upsert '{path}' (HTTP {response.StatusCode}). {response.Content}",
                    null);
        }

        List<string> BuildSchemas(bool includeWebshop)
        {
            var list = new List<string> { m_RequiredSchema };
            if (includeWebshop)
                list.Add(m_WebshopSchema);
            return list;
        }

        void PreserveFromExisting(CatalogItemDto dto, TransportResult exists)
        {
            if (!exists.IsSuccess || string.IsNullOrEmpty(exists.Content))
                return;

            CatalogItemDto existingDto;
            try
            {
                existingDto = JsonConvert.DeserializeObject<CatalogItemDto>(exists.Content, k_DeserSettings);
            }
            catch (Exception e)
            {
                m_Logger.LogWarning(
                    $"Could not parse existing remote item to preserve unknown fields; proceeding without preservation. {e.Message}");
                return;
            }

            if (existingDto is null)
                return;

            dto.PreserveRoundTripFields(existingDto);
        }

        public async Task Delete(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            try
            {
                await DeleteOneWithRetry(catalogItem, cancellationToken);
            }
            catch (ClientException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ClientException($"Delete() failed unexpectedly. {e.Message}", e);
            }
        }

        async Task DeleteOneWithRetry(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            var path = GetCatalogListingPath(catalogItem.CatalogListingId);
            var response = await SendWithRetry(
                () => m_Transport.DeleteConfigAsync(path, cancellationToken),
                cancellationToken);

            if (!response.IsSuccess)
                throw new ClientException(
                    $"Failed to delete '{path}' (HTTP {response.StatusCode}). {response.Content}",
                    null);
        }
    }
}
