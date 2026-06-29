using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing.Editor.Authoring.LiveContentAdminApi
{
    [DataContract]
    public class CatalogItemDto
    {
        [DataMember(Name = "$schema"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IList<string> Schemas { get; set; }
        [DataMember(Name = "uSKU")]
        public string uSku { get; set; }
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ProductType ProductType { get; set; }
        [DataMember(Name = "productDetails")]
        public List<ProductDetailsDto> ProductDetails { get; set; }
        [DataMember(Name = "pricing")]
        public List<PricingDetailsDto> PricingDetails { get; set; }
        [DataMember(Name = "imageUrl"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ImageUrl  { get; set; }
        [DataMember(Name = "storeIdOverrides"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StoreIdOverrideDto> StoreIdOverrides { get; set; }

        [DataMember(Name = "categories"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Categories { get; set; }
        [DataMember(Name = "hdImages"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<HdImageDto> HdImages { get; set; }
        [DataMember(Name = "promotion"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public PromotionDto Promotion { get; set; }

        // Captures any remaining top-level JSON keys this DTO doesn't model — keeps the upsert
        // round trip lossless if a future schema adds new top-level fields. The webshop additions
        // are now typed above (Categories / HdImages / Promotion) and won't land here.
        [JsonExtensionData]
        internal IDictionary<string, JToken> AdditionalProperties { get; set; }
    }

    [Serializable, DataContract]
    public class ProductDetailsDto
    {
        [DataMember(Name = "title")]
        public string Title;
        [DataMember(Name = "description"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Description;
        [DataMember(Name = "language")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TranslationLocale Language;
        [DataMember(Name = "subtitle"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Subtitle;
        [DataMember(Name = "badge"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ProductBadgeDto Badge;
    }

    [Serializable, DataContract]
    public class ProductBadgeDto
    {
        [DataMember(Name = "text")]
        public string Text;
        [DataMember(Name = "imageUrl"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ImageUrl;
    }

    [Serializable, DataContract]
    public class PricingDetailsDto
    {
        [DataMember(Name = "currencyCode")]
        public string CurrencyCode;
        [DataMember(Name = "amount")]
        public long Amount;
        [DataMember(Name = "webshopPrice"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? WebshopPrice;
    }


    public enum ProductType
    {
        Consumable,
        NonConsumable,
        Subscription,
        [EnumMember(Value="non-consumable")]
        NonConsumable2, // TODO: Remove before release, just needed for the current transition
        Unknown
    }

    [Serializable, DataContract]
    public class StoreIdOverrideDto
    {
        [DataMember(Name = "store")]
        [JsonConverter(typeof(StringEnumConverter))]
        public StoreId Store;
        [DataMember(Name = "value")]
        public string Value;
    }

    public enum StoreId
    {
        [EnumMember(Value = "apple")]
        Apple,
        [EnumMember(Value = "google")]
        Google,
        [EnumMember(Value = "xbox")]
        XboxStore,
        [EnumMember(Value = "applemacos")]
        MacAppStore,
    }

    [Serializable, DataContract]
    public class HdImageDto
    {
        [DataMember(Name = "url")]
        public string Url;
        [DataMember(Name = "altText"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AltText;
    }

    [Serializable, DataContract]
    public class PromotionDto
    {
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PromotionTypeDto Type;
        [DataMember(Name = "startsAt"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartsAt;
        [DataMember(Name = "endsAt"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? EndsAt;
    }

    public enum PromotionTypeDto
    {
        Sale,
        Bonus,
        Limited,
    }
}
