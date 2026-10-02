using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Retry;
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

        const string k_SchemaRegistryBasePathProduction =
            "https://services.api.unity.com/schema-registry";
        internal const string k_RequiredSchemaPath =
            "/v1/schemas/UnityRemoteCatalog/versions/1.1.0";
        internal const string k_WebshopSchemaPath =
            "/v1/schemas/UnityRemoteCatalogWebshop/versions/1.1.0";
        internal const string k_WebshopMarker = "UnityRemoteCatalogWebshop";

        // Partial-path match — survives prod/staging base-URL differences.
        internal static bool IsSdkControlled(string url) =>
            url != null && (url.Contains(k_RequiredSchemaPath) || url.Contains(k_WebshopMarker));

        readonly OperationRetryPolicy m_RetryPolicy = OperationRetryPolicy.Create()
            .WithMaxRetries(3)
            .WithBackoff(1000, 8000)
            .WithJitter(250);

        static readonly JsonSerializerSettings k_DtoSettings = new() { Formatting = Formatting.Indented };
        static readonly JsonSerializerSettings k_DeserializationSettings = new() { MissingMemberHandling = MissingMemberHandling.Ignore };

        readonly ILogger m_Logger;
        readonly ILiveContentApiTransport m_Transport;
        readonly string m_RequiredSchema;
        readonly string m_WebshopSchema;

        public LiveContentConfigClient(ILiveContentApiTransport transport, ILogger logger)
            : this(transport, logger, k_SchemaRegistryBasePathProduction) { }

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
                var configs = await FetchAllConfigsWithContent(cancellationToken);

                var result = new List<CatalogItem>();
                foreach (var config in configs)
                {
                    if (config.Body == null || config.VariantTags.Count != 0)
                    {
                        continue;
                    }

                    try
                    {
                        if (!config.Body.TryGetContentAs<CatalogItemDto>(out var dto, k_DeserializationSettings) || dto == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(dto.uSku))
                        {
                            m_Logger.LogWarning($"Config at '{config.Path}' has empty or missing uSku. Skipping.");
                            continue;
                        }

                        var item = dto.ToCatalogItem();
                        item.CatalogListingId = config.Path;
                        result.Add(item);
                    }
                    catch (Exception e)
                    {
                        m_Logger.LogWarning($"Failed to deserialize config at '{config.Path}'. Skipping. {e.Message}");
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

        public async Task<CatalogItem> Get(string catalogListingId, CancellationToken cancellationToken)
        {
            try
            {
                var path = GetCatalogListingPath(catalogListingId);
                var body = await FetchConfigBody(path, cancellationToken);
                return body != null ? ParseCatalogItem(body, path) : null;
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
                throw new ClientException($"Get() failed unexpectedly. {e.Message}", e);
            }
        }

        async Task<LiveContentConfigBody> FetchConfigBody(string path, CancellationToken cancellationToken)
        {
            var result = await SendWithRetry(
                () => m_Transport.GetConfigContentAsync(path, cancellationToken),
                cancellationToken);

            if (result.StatusCode == 404 || !result.IsSuccess)
                return null;

            return result.Content;
        }

        CatalogItem ParseCatalogItem(LiveContentConfigBody body, string path)
        {
            if (!body.TryGetContentAs<CatalogItemDto>(out var dto, k_DeserializationSettings)
                || dto == null || string.IsNullOrEmpty(dto.uSku))
                return null;

            var item = dto.ToCatalogItem();
            item.CatalogListingId = path;
            return item;
        }

        async Task<TResult> SendWithRetry<TResult>(Func<Task<TResult>> sendRequest, CancellationToken cancellationToken)
            where TResult : ITransportResult
        {
            return await m_RetryPolicy.ExecuteAsync(
                sendRequest,
                response => IsTransientFailure(response),
                cancellationToken,
                response => ParseRetryAfter(response));
        }

        static bool IsTransientFailure(ITransportResult response)
        {
            return response.StatusCode == 0
                || response.StatusCode == 429
                || response.StatusCode is >= 500 and < 600;
        }

        static TimeSpan? ParseRetryAfter(ITransportResult response)
        {
            var header = GetHeaderValue(response, "Retry-After");
            if (!string.IsNullOrEmpty(header)
                && int.TryParse(header, out var seconds)
                && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            return null;
        }

        async Task<List<LiveContentConfig>> FetchAllConfigsWithContent(CancellationToken cancellationToken)
        {
            const int maxConfigsApiPageSize = 100;
            var configs = new List<LiveContentConfig>();
            string afterCursor = null;
            var isFirstPage = true;
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);

            do
            {
                var configsResponse = await SendWithRetry(
                    () => m_Transport.GetConfigsContentAsync(
                        pathPrefix: "catalog/",
                        limit: maxConfigsApiPageSize,
                        after: afterCursor,
                        start: isFirstPage ? true : null,
                        schema: m_RequiredSchema,
                        cancellationToken: cancellationToken),
                    cancellationToken);

                isFirstPage = false;

                if (configsResponse.StatusCode == 404)
                    break;

                if (!configsResponse.IsSuccess)
                    throw new ClientException($"GetConfigsContent failed (HTTP {configsResponse.StatusCode}). {configsResponse.Error}", null);

                configs.AddRange(configsResponse.Content);

                afterCursor = GetHeaderValue(configsResponse, "X-Next-Cursor");

                VerifyNotSeenOrThrow(afterCursor, seenCursors);
            } while (!string.IsNullOrEmpty(afterCursor));

            return configs;
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
                    $"Failed to check existence of '{path}' (HTTP {exists.StatusCode}). {exists.Error}",
                    null);

            var dto = catalogItem.ToDto();
            dto.Schemas = BuildSchemas(catalogItem.IsWebshopAvailable);
            PreserveFromExisting(dto, exists);
            dto.Metadata = LiveContentMetadata.ApplyManagedBy(dto.Metadata);

            var jsonContent = JsonConvert.SerializeObject(dto, k_DtoSettings);

            TransportResult<LiveContentConfig> response;
            if (exists.IsSuccess && exists.Content != null)
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
                    $"Failed to upsert '{path}' (HTTP {response.StatusCode}). {response.Error}",
                    null);
        }

        List<string> BuildSchemas(bool includeWebshop)
        {
            var list = new List<string> { m_RequiredSchema };
            if (includeWebshop)
                list.Add(m_WebshopSchema);
            return list;
        }

        void PreserveFromExisting(CatalogItemDto dto, TransportResult<LiveContentConfigBody> exists)
        {
            if (!exists.IsSuccess || exists.Content == null)
                return;

            if (!exists.Content.TryGetContentAs<CatalogItemDto>(out var existingDto, k_DeserializationSettings))
            {
                m_Logger.LogWarning(
                    "Could not parse existing remote item to preserve unknown fields; proceeding without preservation.");
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
                    $"Failed to delete '{path}' (HTTP {response.StatusCode}). {response.Error}",
                    null);
        }

        static string GetHeaderValue(ITransportResult response, string headerName) =>
            response.Headers?
                .FirstOrDefault(h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase)).Value;

        static void VerifyNotSeenOrThrow(string afterCursor, HashSet<string> seenCursors)
        {
            if (!string.IsNullOrEmpty(afterCursor) && !seenCursors.Add(afterCursor))
                throw new ClientException(
                    $"GetConfigsContent returned a previously seen X-Next-Cursor '{afterCursor}'.",
                    null);
        }
    }
}
