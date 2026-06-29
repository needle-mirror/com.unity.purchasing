using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    [DataContract]
    public partial class CatalogItem
    {
        [DataMember(Name = "uSKU"), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string uSku { get; set; }
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ProductType ProductType { get; set; }
        [DataMember(Name = "productDetails")]
        public List<ProductDetails> ProductDetails { get; set; }
        [DataMember(Name = "pricing")]
        public List<PricingDetails> PricingDetails { get; set; }
        [DataMember(Name = "imageUrl")]
        public string ImageUrl  { get; set; }
        // SDK-internal opt-in flag for the Webshop schema. Drives whether ConvertToDto adds
        // the UnityRemoteCatalogWebshop schema URL and emits Categories/HdImages/Promotion.
        // Persists to the .ucat asset; never sent to the admin API (the DTO doesn't model it,
        // server derives state from $schema presence).
        [DataMember(Name = "isWebshopAvailable"), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsWebshopAvailable { get; set; }
        [DataMember(Name = "categories"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Categories { get; set; }
        [DataMember(Name = "hdImages"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<HdImage> HdImages { get; set; }
        [DataMember(Name = "promotion"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Promotion Promotion { get; set; }
        [DataMember(Name = "storeIdOverrides"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StoreIdOverride> StoreIdOverrides { get; set; }

        // Persistent identifier as stored remotely i.e.: "catalog/{filename-no-ext}";
        // CSV: from the CatalogListingId column, fallback to Sku column). Not serialized to
        // the ucat JSON body — its source of truth is the file name or CSV column.
        [IgnoreDataMember, JsonIgnore]
        public string CatalogListingId { get; set; }

        public CatalogItem() { }

        public CatalogItem(CatalogItem catalogItem)
        {
            uSku = catalogItem.uSku;
            CatalogListingId = catalogItem.CatalogListingId;
            ProductType = catalogItem.ProductType;
            ProductDetails = new List<ProductDetails>();
            foreach (var productDetail in catalogItem.ProductDetails)
            {
                ProductDetails.Add(new ProductDetails(productDetail));
            }
            PricingDetails = new List<PricingDetails>();
            foreach (var productDetail in catalogItem.PricingDetails)
            {
                PricingDetails.Add(new PricingDetails(productDetail));
            }
            ImageUrl = catalogItem.ImageUrl;
            IsWebshopAvailable = catalogItem.IsWebshopAvailable;
            if (catalogItem.Categories != null)
            {
                Categories = new List<string>(catalogItem.Categories);
            }
            if (catalogItem.HdImages != null)
            {
                HdImages = new List<HdImage>();
                foreach (var hdImage in catalogItem.HdImages)
                {
                    HdImages.Add(new HdImage(hdImage));
                }
            }
            if (catalogItem.Promotion != null)
            {
                Promotion = new Promotion(catalogItem.Promotion);
            }
            if (catalogItem.StoreIdOverrides != null)
            {
                StoreIdOverrides = new List<StoreIdOverride>();
                foreach (var storeIdOverride in catalogItem.StoreIdOverrides)
                {
                    StoreIdOverrides.Add(new StoreIdOverride(storeIdOverride));
                }
            }
        }

        public static CatalogItem CreateDefaultCatalog()
        {
            var catalog = new CatalogItem
            {
                ProductType = ProductType.Consumable,
                ProductDetails = new List<ProductDetails>()
                {
                    new ProductDetails()
                    {
                        Description = "Description",
                        Language = TranslationLocale.en_US,
                        Title = "Title",
                    }
                },
                PricingDetails = new List<PricingDetails>()
                {
                    new PricingDetails()
                    {
                        Amount = 4.99,
                        CurrencyCode = "USD"
                    }
                }
            };

            return catalog;
        }
    }

    [Serializable, DataContract]
    public class ProductDetails
    {
        [DataMember(Name = "title")]
        public string Title;
        [DataMember(Name = "description")]
        public string Description;
        [DataMember(Name = "language")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TranslationLocale Language;
        [DataMember(Name = "subtitle"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Subtitle;
        [DataMember(Name = "badge"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ProductBadge Badge;

        public ProductDetails() { }

        public ProductDetails(ProductDetails other)
        {
            Title = other.Title;
            Description = other.Description;
            Language = other.Language;
            Subtitle = other.Subtitle;
            Badge = other.Badge == null ? null : new ProductBadge(other.Badge);
        }
    }

    [Serializable, DataContract]
    public class ProductBadge
    {
        [DataMember(Name = "text")]
        public string Text;
        [DataMember(Name = "imageUrl"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ImageUrl;

        public ProductBadge() { }

        public ProductBadge(ProductBadge other)
        {
            Text = other.Text;
            ImageUrl = other.ImageUrl;
        }
    }

    [Serializable, DataContract]
    public class StoreIdOverride
    {
        [DataMember(Name = "store")]
        [JsonConverter(typeof(StringEnumConverter))]
        public StoreId Store;
        [DataMember(Name = "value")]
        public string Value;

        public StoreIdOverride() { }

        public StoreIdOverride(StoreIdOverride other)
        {
            Store = other.Store;
            Value = other.Value;
        }
    }

    [Serializable, DataContract]
    public class PricingDetails
    {
        // Below this threshold WebshopPrice is treated as unset (Unity's inspector can't render
        // Nullable<T>, so we use the value itself as the on/off signal instead of a sibling bool).
        // Sub-cent values aren't a real commercial price.
        public const double WebshopPriceUnsetThreshold = 0.001;

        [DataMember(Name = "currencyCode")]
        public string CurrencyCode;
        [DataMember(Name = "amount")]
        public double Amount;
        [DataMember(Name = "webshopPrice")]
        public double WebshopPrice;

        [JsonIgnore]
        public bool IsWebshopPriceSet => WebshopPrice >= WebshopPriceUnsetThreshold;

        public bool ShouldSerializeWebshopPrice() => IsWebshopPriceSet;

        public PricingDetails() { }

        public PricingDetails(PricingDetails other)
        {
            Amount = other.Amount;
            CurrencyCode = other.CurrencyCode;
            WebshopPrice = other.WebshopPrice;
        }
    }

    [Serializable, DataContract]
    public class HdImage
    {
        [DataMember(Name = "url")]
        public string Url;
        [DataMember(Name = "altText"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AltText;

        public HdImage() { }

        public HdImage(HdImage other)
        {
            Url = other.Url;
            AltText = other.AltText;
        }
    }

    [Serializable, DataContract]
    public class Promotion
    {
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PromotionType Type;
        [DataMember(Name = "startsAt"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartsAt;
        [DataMember(Name = "endsAt"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? EndsAt;

        public Promotion() { }

        public Promotion(Promotion other)
        {
            Type = other.Type;
            StartsAt = other.StartsAt;
            EndsAt = other.EndsAt;
        }
    }

    public enum PromotionType
    {
        Sale,
        Bonus,
        Limited,
    }
}
