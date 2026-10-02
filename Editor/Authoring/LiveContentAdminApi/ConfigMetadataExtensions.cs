using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Purchasing.Editor.Shared.WebApi;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    /// <summary>
    /// Flattens generated Admin API config documents into one <see cref="LiveContentConfig"/> per
    /// variant. <see cref="ConfigMetadata"/> yields metadata-only configs;
    /// <see cref="ConfigMetadataWithContent"/> yields configs with an inline body.
    /// </summary>
    internal static class ConfigMetadataExtensions
    {
        internal static IReadOnlyList<LiveContentConfig> ToLiveContentConfigs(this ConfigMetadata configMetadata)
        {
            if (configMetadata == null)
                throw new ArgumentNullException(nameof(configMetadata));
            if (configMetadata.Variants == null)
                throw new ArgumentException("Config metadata variants cannot be null.", nameof(configMetadata));
            if (configMetadata.Type != ConfigMetadata.TypeEnum.Config)
                throw new ArgumentException("Config metadata types must be ConfigMetadata type.", nameof(configMetadata));

            return configMetadata.Variants
                .Select(variant => ToLiveContentConfig(configMetadata, variant))
                .ToList();
        }

        internal static IReadOnlyList<LiveContentConfig> ToLiveContentConfigs(this ConfigMetadataWithContent configMetadata)
        {
            if (configMetadata == null)
                throw new ArgumentNullException(nameof(configMetadata));
            if (configMetadata.Variants == null)
                throw new ArgumentException("Config metadata variants cannot be null.", nameof(configMetadata));
            if (configMetadata.Type != ConfigMetadataWithContent.TypeEnum.Config)
                throw new ArgumentException("Config metadata types must be ConfigMetadata type.", nameof(configMetadata));

            return configMetadata.Variants
                .Select(variant => ToLiveContentConfig(configMetadata, variant))
                .ToList();
        }

        static LiveContentConfig ToLiveContentConfig(ConfigMetadata configMetadata, ConfigVariant variant)
        {
            if (variant == null)
                throw new ArgumentException("Config metadata cannot contain a null variant.", nameof(variant));

            return new LiveContentConfig(
                id: configMetadata.Id,
                path: configMetadata.Path,
                contentType: configMetadata.ContentType,
                sortIndex: configMetadata.SortIndex,
                schemas: Copy(configMetadata.Schemas),
                variantTag: Copy(variant.VariantTag),
                contentHash: variant.ContentHash,
                contentSize: variant.ContentSize,
                complete: variant.Complete,
                createdAt: variant.CreatedAt,
                updatedAt: variant.UpdatedAt,
                metadata: ConvertMetadata(variant.Metadata));
        }

        static LiveContentConfig ToLiveContentConfig(
            ConfigMetadataWithContent configMetadata,
            ConfigVariantWithContent variant)
        {
            if (variant == null)
                throw new ArgumentException("Config metadata cannot contain a null variant.", nameof(variant));

            return new LiveContentConfig(
                id: configMetadata.Id,
                path: configMetadata.Path,
                contentType: configMetadata.ContentType,
                sortIndex: configMetadata.SortIndex,
                schemas: Copy(configMetadata.Schemas),
                variantTag: Copy(variant.VariantTag),
                contentHash: variant.ContentHash,
                contentSize: variant.ContentSize,
                complete: variant.Complete,
                createdAt: variant.CreatedAt,
                updatedAt: variant.UpdatedAt,
                metadata: ConvertMetadata(variant.Metadata),
                body: variant.Content?.ToLiveContentContent());
        }

        // Bridges the generated-client body (ApiObject-valued dictionary) into the domain's
        // transport-agnostic JObject body.
        internal static LiveContentConfigBody ToLiveContentContent(this Dictionary<string, ApiObject> values)
        {
            return new LiveContentConfigBody(values == null ? new JObject() : JObject.FromObject(values));
        }

        static List<string> Copy(List<string> source) =>
            source == null ? new List<string>() : new List<string>(source);

        static Dictionary<string, object> ConvertMetadata(Dictionary<string, ApiObject> metadata)
        {
            var convertedMetadata = new Dictionary<string, object>();
            if (metadata == null)
                return convertedMetadata;

            foreach (var entry in metadata)
                convertedMetadata[entry.Key] = entry.Value?.GetAs<object>();

            return convertedMetadata;
        }
    }
}
