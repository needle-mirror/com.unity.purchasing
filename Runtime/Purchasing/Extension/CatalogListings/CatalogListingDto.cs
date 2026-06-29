#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.CatalogListings
{
    [Preserve]
    internal class JsonStringProductTypeConverter : JsonConverter<CatalogProductType>
    {
        public override void WriteJson(JsonWriter writer, CatalogProductType value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override CatalogProductType ReadJson(JsonReader reader, Type objectType, CatalogProductType existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return (reader.Value as string)?.ToLowerInvariant() switch
            {
                "consumable" => CatalogProductType.Consumable,
                "nonconsumable" => CatalogProductType.NonConsumable,
                "subscription" => CatalogProductType.Subscription,
                _ => CatalogProductType.Unknown
            };
        }
    }

    [Preserve]
    internal class JsonStringStoreConverter : JsonConverter<CatalogStore>
    {
        public override void WriteJson(JsonWriter writer, CatalogStore value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override CatalogStore ReadJson(JsonReader reader, Type objectType, CatalogStore existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            return (reader.Value as string)?.ToLowerInvariant() switch
            {
                "apple" => CatalogStore.Apple,
                "google" => CatalogStore.Google,
                "applemacos" => CatalogStore.AppleMacos,
                "xbox" => CatalogStore.Xbox,
                _ => CatalogStore.Unknown
            };
        }
    }

    [Preserve]
    [JsonConverter(typeof(JsonStringProductTypeConverter))]
    internal enum CatalogProductType
    {
        Unknown,
        Consumable,
        NonConsumable,
        Subscription
    }

    [Preserve]
    [JsonConverter(typeof(JsonStringStoreConverter))]
    internal enum CatalogStore
    {
        Unknown,
        Apple,
        Google,
        AppleMacos,
        Xbox
    }

    [Preserve]
    [DataContract]
    internal class CatalogListingDto
    {
        [Preserve]
        [DataMember(Name = "uSKU", IsRequired = true)] public string USku { get; set; } = null!;

        [Preserve]
        [DataMember(Name = "type")]
        public CatalogProductType Type { get; set; } = CatalogProductType.Unknown;

        [Preserve]
        [DataMember(Name = "productDetails")]
        public List<ProductDetailDto> ProductDetails { get; set; } = new List<ProductDetailDto>();

        [Preserve]
        [DataMember(Name = "pricing")] public List<PricingDto>? Pricing { get; set; }

        [Preserve]
        [DataMember(Name = "imageUrl")] public string? ImageUrl { get; set; }

        [Preserve]
        [DataMember(Name = "storeIdOverrides")]
        public List<StoreIdOverrideDto> StoreIdOverrides { get; set; } = new List<StoreIdOverrideDto>();

        /// <summary>
        /// The catalog listing's identifier within LiveContent. Not present in the JSON content body;
        /// populated from the LiveContent envelope (currently the config path; may switch to a more
        /// appropriate field later).
        /// </summary>
        public string? CatalogListingId { get; set; }

        /// <summary>
        /// True when the item has a corresponding Webshop item.
        /// </summary>
        public bool HasWebshop { get; set; }
    }

    [Preserve]
    [DataContract]
    internal class PricingDto
    {
        [Preserve]
        [DataMember(Name = "currencyCode")] public string? CurrencyCode { get; set; }

        [Preserve]
        [DataMember(Name = "amount")]
        public long Amount { get; set; } = 0;

        [Preserve]
        [DataMember(Name = "webshopPrice")] public long? WebshopPrice { get; set; }
    }

    [Preserve]
    [DataContract]
    internal class ProductDetailDto
    {
        [Preserve]
        [DataMember(Name = "language")] public string? Language { get; set; }

        [Preserve]
        [DataMember(Name = "title")] public string? Title { get; set; }

        [Preserve]
        [DataMember(Name = "subtitle")] public string? Subtitle { get; set; }

        [Preserve]
        [DataMember(Name = "description")] public string? Description { get; set; }

        [Preserve]
        [DataMember(Name = "badge")] public BadgeDto? Badge { get; set; }
    }

    [Preserve]
    [DataContract]
    internal class BadgeDto
    {
        [Preserve]
        [DataMember(Name = "text")] public string? Text { get; set; }

        [Preserve]
        [DataMember(Name = "imageUrl")] public string? ImageUrl { get; set; }
    }

    [Preserve]
    [DataContract]
    internal class StoreIdOverrideDto
    {
        [Preserve]
        [DataMember(Name = "store")] public CatalogStore? Store { get; set; }

        [Preserve]
        [DataMember(Name = "value")] public string? Value { get; set; }
    }
}
