using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Purchasing.Editor.Shared.Clients;
using Unity.Purchasing.Editor.Shared.WebApi;
using Unity.Services.Core.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Logger;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;
using UnityEditor.Purchasing.Editor.Authoring.Model.Extensions;
using CoreProductType = UnityEditor.Purchasing.Editor.Authoring.Core.ProductType;
using CoreStoreId = UnityEditor.Purchasing.Editor.Authoring.Core.StoreId;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    class LiveContentConfigClient : ILiveContentConfigClient
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

        const string k_RequiredSchema =
            "https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog/versions/1.1.0";
        const string k_WebshopSchema =
            "https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalogWebshop/versions/1.1.0";
        // Substring used to detect the webshop schema on round trip (mirrors the runtime
        // CatalogListingParser convention — survives version bumps without an exact-string match).
        const string k_WebshopSchemaMarker = "UnityRemoteCatalogWebshop";
        const int k_MaxConcurrentFetches = 8;
        const int k_MaxFetchRetries = 4;
        static readonly Random s_Jitter = new Random();
        readonly ILogger m_Logger;
        readonly IAccessTokens m_TokenProvider;
        readonly IConfigsApi m_ConfigsApi;
        string m_EnvironmentId;
        string m_ProjectId;
        const string k_MetadataKey = "$metadata";
        const string k_ManagedByKey = "managedBy";
        const string k_ManagedByValue = "In App Purchase";

        public LiveContentConfigClient(
            ILogger logger,
            IAccessTokens tokenProviders,
            IConfigsApi configsApi)
        {
            m_Logger = logger;
            m_TokenProvider = tokenProviders;
            m_ConfigsApi = configsApi;
        }

        public async Task Initialize(
            string environmentId,
            string projectId,
            CancellationToken cancellationToken)
        {
            await UpdateToken();
            m_EnvironmentId = environmentId;
            m_ProjectId = projectId;
        }

        public async Task<List<CatalogItem>> List(CancellationToken cancellationToken)
        {
            await UpdateToken();
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

                    if (!payload.IsSuccessful)
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

                    // The server-side schema filter on GetConfigs should already exclude items
                    // that don't match UnityRemoteCatalog/1.1.0, so any deserialization or
                    // shape failure here is technically a server/contract violation. We treat
                    // it as a non-fatal warning so one bad item can't break List() for the rest.
                    try
                    {
                        var dto = IsolatedJsonConvert.DeserializeObject<CatalogItemDto>(payload.Content);
                        if (dto == null)
                            continue;
                        if (string.IsNullOrEmpty(dto.uSku))
                        {
                            m_Logger.LogWarning($"Config at '{configPath}' has empty or missing uSku. Skipping.");
                            continue;
                        }
                        var item = ConvertFromDto(dto);
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
            catch (ApiException e)
            {
                throw GetRequestException(e);
            }
        }

        async Task<ApiResponse> FetchConfigWithRetry(string configPath, SemaphoreSlim gate, CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await SendWithRetry(
                    () => m_ConfigsApi.GetConfigContent(
                        m_EnvironmentId,
                        m_ProjectId,
                        configPath,
                        cancellationToken: cancellationToken),
                    cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        async Task<ApiResponse> SendWithRetry(Func<ApiOperation> sendRequest, CancellationToken cancellationToken)
        {
            ApiResponse response = null;
            for (var attempt = 0; attempt < k_MaxFetchRetries; attempt++)
            {
                response = await sendRequest();

                if (response.IsSuccessful || response.StatusCode == 404 || !IsTransientFailure(response))
                    return response;

                if (attempt < k_MaxFetchRetries - 1)
                {
                    var delay = ComputeRetryDelay(response, attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            return response;
        }

        static bool IsTransientFailure(ApiResponse response)
        {
            return response.StatusCode == 0
                || response.StatusCode == 429
                || (response.StatusCode >= 500 && response.StatusCode < 600);
        }

        static TimeSpan ComputeRetryDelay(ApiResponse response, int attempt)
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
                var configsResponse = await m_ConfigsApi.GetConfigs(
                    m_EnvironmentId,
                    m_ProjectId,
                    path: "catalog/",
                    limit: maxConfigsApiPageSize,
                    after: afterCursor,
                    start: isFirstPage ? true : (bool?)null,
                    schema: k_RequiredSchema,
                    noVariantTag: true,
                    cancellationToken: cancellationToken);

                isFirstPage = false;

                if (configsResponse.StatusCode == 404 || string.IsNullOrEmpty(configsResponse.Content))
                {
                    break;
                }
                if (configsResponse.StatusCode < 200 || configsResponse.StatusCode >= 300)
                    throw GetRequestException(configsResponse);

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
            await UpdateToken();
            try
            {
                await UpsertOneWithRetry(catalogItem, cancellationToken);
            }
            catch (ApiException e)
            {
                throw GetRequestException(e);
            }
        }

        async Task UpsertOneWithRetry(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            var path = GetCatalogListingPath(catalogItem.CatalogListingId);

            var exists = await SendWithRetry(
                () => m_ConfigsApi.GetConfigContent(
                    m_EnvironmentId,
                    m_ProjectId,
                    path,
                    cancellationToken: cancellationToken),
                cancellationToken);

            if (!exists.IsSuccessful && exists.StatusCode != 404)
                throw GetRequestException(exists);

            var dto = ConvertToDto(catalogItem);
            PreserveFromExisting(dto, exists);

            var json = IsolatedJsonConvert.SerializeObject(dto,
                new JsonSerializerSettings { Formatting = Formatting.Indented });
            
            var body = IsolatedJsonConvert.DeserializeObject<Dictionary<string, ApiObject>>(json);

            AddManagedByMetadata(body, exists);

            ApiResponse response;
            if (exists.IsSuccessful && !string.IsNullOrEmpty(exists.Content))
            {
                response = await SendWithRetry(
                    () => m_ConfigsApi.UpdateConfigFile(
                        m_EnvironmentId,
                        m_ProjectId,
                        path,
                        body,
                        cancellationToken: cancellationToken),
                    cancellationToken);
            }
            else
            {
                response = await SendWithRetry(
                    () => m_ConfigsApi.CreateConfigFile(
                        m_EnvironmentId,
                        m_ProjectId,
                        path,
                        body,
                        cancellationToken: cancellationToken),
                    cancellationToken);
            }

            if (!response.IsSuccessful)
                throw GetRequestException(response);
        }

        // Graft from the existing remote payload onto the upsert DTO:
        //   - AdditionalProperties: any top-level JSON keys the SDK doesn't model. Webshop
        //     additions (categories / hdImages / promotion) are now typed on CatalogItemDto and
        //     no longer land here; this graft only carries truly-unknown future additions.
        //   - Schemas: any URLs the SDK doesn't control (forward-compat for future schemas).
        //     The required catalog schema and the webshop schema are SDK-controlled — ConvertToDto
        //     already set the former, and decides the latter based on the local toggle. Stripping
        //     the webshop schema when the local toggle is OFF is what makes the off-state semantic
        //     ("not a webshop item") actually erase server-side data.
        void PreserveFromExisting(CatalogItemDto dto, ApiResponse exists)
        {
            if (!exists.IsSuccessful || string.IsNullOrEmpty(exists.Content))
                return;

            CatalogItemDto existingDto;
            try
            {
                existingDto = IsolatedJsonConvert.DeserializeObject<CatalogItemDto>(exists.Content);
            }
            catch (Exception e)
            {
                m_Logger.LogWarning(
                    $"Could not parse existing remote item to preserve unknown fields; proceeding without preservation. {e.Message}");
                return;
            }

            if (existingDto is null)
                return;

            dto.AdditionalProperties = existingDto.AdditionalProperties;
            ForwardUnknownSchemas(dto, existingDto);
        }

        static void ForwardUnknownSchemas(CatalogItemDto target, CatalogItemDto source)
        {
            if (source.Schemas is null)
                return;
            target.Schemas ??= new List<string>();
            foreach (var s in source.Schemas)
            {
                if (IsSdkControlledSchema(s))
                    continue;
                if (!target.Schemas.Contains(s))
                    target.Schemas.Add(s);
            }
        }

        static bool IsSdkControlledSchema(string url) =>
            url != null && (url == k_RequiredSchema || url.Contains(k_WebshopSchemaMarker));

        public async Task Delete(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            await UpdateToken();
            try
            {
                await DeleteOneWithRetry(catalogItem, cancellationToken);
            }
            catch (ApiException e)
            {
                throw GetRequestException(e);
            }
        }

        async Task DeleteOneWithRetry(CatalogItem catalogItem, CancellationToken cancellationToken)
        {
            var path = GetCatalogListingPath(catalogItem.CatalogListingId);
            var response = await SendWithRetry(
                () => m_ConfigsApi.DeleteConfig(
                    m_EnvironmentId,
                    m_ProjectId,
                    path,
                    cancellationToken: cancellationToken),
                cancellationToken);

            if (!response.IsSuccessful)
                throw GetRequestException(response);
        }

        static void AddManagedByMetadata(Dictionary<string, ApiObject> body, ApiResponse existingContent)
        {
            JObject metadata = null;

            if (existingContent.IsSuccessful && !string.IsNullOrEmpty(existingContent.Content))
            {
                try
                {
                    var existingJson = JObject.Parse(existingContent.Content);
                    metadata = existingJson[k_MetadataKey] as JObject;
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    // Fallback to empty metadata if parsing fails, so we can still upsert/overwrite the file
                }
            }

            metadata ??= new JObject();
            metadata[k_ManagedByKey] = k_ManagedByValue;
            body[k_MetadataKey] = new ApiObject(metadata);
        }

        async Task UpdateToken()
        {
            var client = m_ConfigsApi as ConfigsApi;
            if (client == null)
                return;

            string token = await m_TokenProvider.GetServicesGatewayTokenAsync();
            var headers = new AdminApiHeaders<LiveContentConfigClient>(token);
            client.Configuration.DefaultHeaders = headers.ToDictionary();
        }

        CatalogItem ConvertFromDto(CatalogItemDto i)
        {
            return new CatalogItem
            {
                uSku = i.uSku,
                ProductType = ConvertTypeFromDto(i.ProductType),
                PricingDetails = i.PricingDetails?.Select(ConvertFromDto)?.ToList() ?? new List<PricingDetails>(),
                ProductDetails = i.ProductDetails?.Select(ConvertFromDto)?.ToList() ?? new List<ProductDetails>(),
                ImageUrl = i.ImageUrl,
                StoreIdOverrides = i.StoreIdOverrides?.Select(ConvertFromDto)?.ToList(),
                IsWebshopAvailable = i.Schemas?.Any(s => s != null && s.Contains(k_WebshopSchemaMarker)) ?? false,
                Categories = i.Categories is null ? null : new List<string>(i.Categories),
                HdImages = i.HdImages?.Select(ConvertFromDto)?.ToList(),
                Promotion = ConvertFromDto(i.Promotion),
            };
        }

        static StoreIdOverride ConvertFromDto(StoreIdOverrideDto s) =>
            new StoreIdOverride { Store = ConvertStoreIdFromDto(s.Store), Value = s.Value };

        static ProductDetails ConvertFromDto(ProductDetailsDto pd)
        {
            return new ProductDetails
            {
                Title = pd.Title,
                Description = pd.Description,
                Language = pd.Language.ConvertLanguageCode(),
                Subtitle = pd.Subtitle,
                Badge = pd.Badge == null ? null : new ProductBadge { Text = pd.Badge.Text, ImageUrl = pd.Badge.ImageUrl }
            };
        }

        internal static PricingDetails ConvertFromDto(PricingDetailsDto pd)
        {
            return new PricingDetails
            {
                CurrencyCode = pd.CurrencyCode,
                Amount = pd.Amount / 1_000_000D,
                WebshopPrice = pd.WebshopPrice / 1_000_000D ?? 0
            };
        }

        CatalogItemDto ConvertToDto(CatalogItem i)
        {
            return new CatalogItemDto
            {
                Schemas = BuildSchemas(i.IsWebshopAvailable),
                uSku = i.uSku,
                ProductType = ConvertTypeToDto(i.ProductType),
                PricingDetails = i.PricingDetails?.Select(ConvertToDto)?.ToList() ?? new List<PricingDetailsDto>(),
                ProductDetails = i.ProductDetails?.Select(ConvertToDto)?.ToList() ?? new List<ProductDetailsDto>(),
                ImageUrl = NullIfEmpty(i.ImageUrl),
                StoreIdOverrides = ConvertToDto(i.StoreIdOverrides),
                Categories = i.IsWebshopAvailable ? ConvertCategoriesToDto(i.Categories) : null,
                HdImages = i.IsWebshopAvailable ? ConvertHdImagesToDto(i.HdImages) : null,
                Promotion = i.IsWebshopAvailable ? ConvertToDto(i.Promotion) : null,
            };
        }

        static List<string> BuildSchemas(bool includeWebshop)
        {
            var list = new List<string> { k_RequiredSchema };
            if (includeWebshop)
                list.Add(k_WebshopSchema);
            return list;
        }

        static List<string> ConvertCategoriesToDto(List<string> categories)
        {
            if (categories is null)
                return null;
            var result = new List<string>(categories.Count);
            foreach (var c in categories)
            {
                if (!string.IsNullOrEmpty(c))
                    result.Add(c);
            }
            return result.Count == 0 ? null : result;
        }

        static List<HdImageDto> ConvertHdImagesToDto(List<HdImage> images)
        {
            if (images is null)
                return null;
            var result = new List<HdImageDto>(images.Count);
            foreach (var img in images)
            {
                if (img is null || string.IsNullOrEmpty(img.Url))
                    continue;
                result.Add(new HdImageDto { Url = img.Url, AltText = NullIfEmpty(img.AltText) });
            }
            return result.Count == 0 ? null : result;
        }

        static HdImage ConvertFromDto(HdImageDto h) =>
            new HdImage { Url = h.Url, AltText = h.AltText };

        static PromotionDto ConvertToDto(Promotion p) =>
            p is null ? null : new PromotionDto
            {
                Type = ConvertPromotionTypeToDto(p.Type),
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
            };

        static Promotion ConvertFromDto(PromotionDto p) =>
            p is null ? null : new Promotion
            {
                Type = ConvertPromotionTypeFromDto(p.Type),
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
            };

        static PromotionTypeDto ConvertPromotionTypeToDto(PromotionType t)
        {
            switch (t)
            {
                case PromotionType.Sale: return PromotionTypeDto.Sale;
                case PromotionType.Bonus: return PromotionTypeDto.Bonus;
                case PromotionType.Limited: return PromotionTypeDto.Limited;
                default: throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        static PromotionType ConvertPromotionTypeFromDto(PromotionTypeDto t)
        {
            switch (t)
            {
                case PromotionTypeDto.Sale: return PromotionType.Sale;
                case PromotionTypeDto.Bonus: return PromotionType.Bonus;
                case PromotionTypeDto.Limited: return PromotionType.Limited;
                default: throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }
        }

        static List<StoreIdOverrideDto> ConvertToDto(List<StoreIdOverride> overrides)
        {
            if (overrides is null)
                return null;
            var result = new List<StoreIdOverrideDto>(overrides.Count);
            foreach (var o in overrides)
            {
                if (o is null || string.IsNullOrEmpty(o.Value))
                    continue;
                result.Add(new StoreIdOverrideDto
                {
                    Store = ConvertStoreIdToDto(o.Store),
                    Value = o.Value
                });
            }
            return result.Count == 0 ? null : result;
        }

        static ProductDetailsDto ConvertToDto(ProductDetails pd)
        {
            return new ProductDetailsDto
            {
                Title = pd.Title,
                // Schema 1.1.0 lists description as optional, but rejects empty strings on
                // upload (length must be >= 1 if present). Normalise empty -> null here so
                // callers can leave the field unset without crafting a special-case DTO.
                Description = NullIfEmpty(pd.Description),
                Language = pd.Language.ConvertLanguageCode(),
                Subtitle = NullIfEmpty(pd.Subtitle),
                Badge = ConvertToDto(pd.Badge)
            };
        }

        static ProductBadgeDto ConvertToDto(ProductBadge badge)
        {
            if (badge is null || string.IsNullOrEmpty(badge.Text))
                return null;
            return new ProductBadgeDto
            {
                Text = badge.Text,
                ImageUrl = NullIfEmpty(badge.ImageUrl)
            };
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

        internal static PricingDetailsDto ConvertToDto(PricingDetails pd)
        {
            return new PricingDetailsDto
            {
                CurrencyCode = pd.CurrencyCode,
                Amount = ToMicros(pd.Amount),
                WebshopPrice = pd.IsWebshopPriceSet ? ToMicros(pd.WebshopPrice) : null
            };
        }

        static long ToMicros(double amount) =>
            checked((long)Math.Round(amount * 1_000_000D, MidpointRounding.AwayFromZero));

        ProductType ConvertTypeToDto(CoreProductType productType)
        {
            switch(productType)
            {
                case CoreProductType.Consumable:
                    return ProductType.Consumable;
                case CoreProductType.NonConsumable:
                    return ProductType.NonConsumable;
                case CoreProductType.Subscription:
                    return ProductType.Subscription;
                case CoreProductType.Unknown:
                    return ProductType.Unknown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(productType), productType, null);
            }
        }

        static StoreId ConvertStoreIdToDto(CoreStoreId store)
        {
            switch (store)
            {
                case CoreStoreId.Apple: return StoreId.Apple;
                case CoreStoreId.Google: return StoreId.Google;
                case CoreStoreId.XboxStore: return StoreId.XboxStore;
                case CoreStoreId.MacAppStore: return StoreId.MacAppStore;
                default: throw new ArgumentOutOfRangeException(nameof(store), store, null);
            }
        }

        static CoreStoreId ConvertStoreIdFromDto(StoreId store)
        {
            switch (store)
            {
                case StoreId.Apple: return CoreStoreId.Apple;
                case StoreId.Google: return CoreStoreId.Google;
                case StoreId.XboxStore: return CoreStoreId.XboxStore;
                case StoreId.MacAppStore: return CoreStoreId.MacAppStore;
                default: throw new ArgumentOutOfRangeException(nameof(store), store, null);
            }
        }



        CoreProductType ConvertTypeFromDto(ProductType productType)
        {
            switch (productType)
            {
                case ProductType.Consumable:
                    return CoreProductType.Consumable;
                case ProductType.NonConsumable:
                case ProductType.NonConsumable2:
                    return CoreProductType.NonConsumable;
                case ProductType.Subscription:
                    return CoreProductType.Subscription;
                case ProductType.Unknown:
                    return CoreProductType.Unknown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(productType), productType, null);
            }
        }

        static ClientException GetRequestException(ApiException e, [CallerMemberName] string caller = null)
        {
            return new ClientException(
                $"Request '{caller} - {e.Response.Url}' failed with '{e.Response.StatusCode}'. "
                + $"{e.Message}", e);
        }

        static ClientException GetRequestException(ApiResponse response, [CallerMemberName] string caller = null)
        {
            return new ClientException(
                $"Request '{caller} - {response.Url}' failed with '{response.StatusCode}'. "
                + $"{response.Content}", null);
        }
    }
}
