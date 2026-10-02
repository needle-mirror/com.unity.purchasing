using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    [DataContract]
    public partial class CatalogItem
    {
        [JsonProperty("$schema", Order = -100)]
        public string Schema => "https://ugs-config-schemas.unity3d.com/v1/purchasing-catalog.schema.json";

        [CsvColumn("Sku", 1, Identity = true)]
        [DataMember(Name = "uSKU"), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string uSku { get; set; }
        [CsvColumn("ProductType", 8, Fallback = ProductType.Consumable)]
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter)), JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public ProductType ProductType { get; set; }
        [CsvRowCollection(
            KeyMember = nameof(Model.ProductDetails.Language),
            RequiredMembers = new[] { nameof(Model.ProductDetails.Title) })]
        [DataMember(Name = "productDetails")]
        public List<ProductDetails> ProductDetails { get; set; }
        [CsvRowCollection(
            KeyMember = nameof(Model.PricingDetails.CurrencyCode),
            IgnoreKeyCase = true,
            RequiredMembers = new[]
            {
                nameof(Model.PricingDetails.CurrencyCode),
                nameof(Model.PricingDetails.Amount),
            })]
        [DataMember(Name = "pricing")]
        public List<PricingDetails> PricingDetails { get; set; }
        [CsvColumn("ImageUrl", 12)]
        [DataMember(Name = "imageUrl")]
        public string ImageUrl  { get; set; }
        // SDK-internal opt-in flag for the Webshop schema. Drives whether ConvertToDto adds
        // the UnityRemoteCatalogWebshop schema URL and emits Categories/HdImages/Promotion.
        // Persists to the .ucat asset; never sent to the admin API (the DTO doesn't model it,
        // server derives state from $schema presence).
        [CsvColumn("IsWebshopAvailable", 17, EmitWhenDefault = false)]
        [DataMember(Name = "isWebshopAvailable"), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsWebshopAvailable { get; set; }
        [CsvColumn("Category", 18)]
        [CsvRowCollection(ReportDuplicates = false, NullWhenEmpty = true)]
        [DataMember(Name = "categories"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Categories { get; set; }
        [CsvRowCollection(
            KeyMember = nameof(HdImage.Url), ReportDuplicates = false, NullWhenEmpty = true)]
        [DataMember(Name = "hdImages"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<HdImage> HdImages { get; set; }
        [CsvNested(RequiredMember = nameof(Model.Promotion.Type))]
        [DataMember(Name = "promotion"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Promotion Promotion { get; set; }
        [DataMember(Name = "storeIdOverrides"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StoreIdOverride> StoreIdOverrides { get; set; }

        public bool ShouldSerializeStoreIdOverrides() => false;

        [CsvColumn("GoogleOverride", 13)]
        [IgnoreDataMember, JsonProperty("googleOverride", NullValueHandling = NullValueHandling.Ignore)]
        string GoogleOverride
        {
            get => GetStoreIdOverride(StoreId.Google);
            set => SetStoreIdOverride(StoreId.Google, value);
        }

        [CsvColumn("AppleOverride", 14)]
        [IgnoreDataMember, JsonProperty("appleOverride", NullValueHandling = NullValueHandling.Ignore)]
        string AppleOverride
        {
            get => GetStoreIdOverride(StoreId.Apple);
            set => SetStoreIdOverride(StoreId.Apple, value);
        }

        [CsvColumn("XboxStoreOverride", 15)]
        [IgnoreDataMember, JsonProperty("xboxStoreOverride", NullValueHandling = NullValueHandling.Ignore)]
        string XboxStoreOverride
        {
            get => GetStoreIdOverride(StoreId.XboxStore);
            set => SetStoreIdOverride(StoreId.XboxStore, value);
        }

        [CsvColumn("MacAppStoreOverride", 16)]
        [IgnoreDataMember, JsonProperty("macAppStoreOverride", NullValueHandling = NullValueHandling.Ignore)]
        string MacAppStoreOverride
        {
            get => GetStoreIdOverride(StoreId.MacAppStore);
            set => SetStoreIdOverride(StoreId.MacAppStore, value);
        }

        public string GetStoreIdOverride(StoreId store)
        {
            return StoreIdOverrides?.FirstOrDefault(entry => entry.Store == store)?.Value;
        }

        public void SetStoreIdOverride(StoreId store, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                StoreIdOverrides?.RemoveAll(entry => entry.Store == store);
                return;
            }
            StoreIdOverrides ??= new List<StoreIdOverride>();
            var existing = StoreIdOverrides.FirstOrDefault(entry => entry.Store == store);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                StoreIdOverrides.Add(new StoreIdOverride { Store = store, Value = value });
            }
        }

        // Persistent identifier as stored remotely i.e.: "catalog/{filename-no-ext}";
        // CSV: from the CatalogListingId column, fallback to Sku column). Not serialized to
        // the ucat JSON body — its source of truth is the file name or CSV column.
        [CsvColumn("CatalogListingId", 0, Identity = true)]
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

        public static List<CatalogItem> CreateDefaultCsvCatalog()
        {
            return new List<CatalogItem>
            {
                new CatalogItem
                {
                    CatalogListingId = "catalog/starter_pack",
                    uSku = "starter_pack",
                    ProductType = ProductType.Consumable,
                    ProductDetails = new List<ProductDetails>
                    {
                        new ProductDetails { Title = "Starter Pack", Description = "A one-time starter bundle.", Language = TranslationLocale.en_US },
                        new ProductDetails { Title = "Pack de démarrage", Description = "Un lot de démarrage.", Language = TranslationLocale.fr_FR },
                    },
                    PricingDetails = new List<PricingDetails>
                    {
                        new PricingDetails { CurrencyCode = "USD", Amount = 4.99 },
                    },
                },
                new CatalogItem
                {
                    CatalogListingId = "catalog/premium_upgrade",
                    uSku = "premium_upgrade",
                    ProductType = ProductType.NonConsumable,
                    ProductDetails = new List<ProductDetails>
                    {
                        new ProductDetails { Title = "Premium Upgrade", Description = "Unlock all premium features.", Language = TranslationLocale.en_US },
                    },
                    PricingDetails = new List<PricingDetails>
                    {
                        new PricingDetails { CurrencyCode = "USD", Amount = 9.99 },
                    },
                },
                new CatalogItem
                {
                    CatalogListingId = "catalog/vip_monthly",
                    uSku = "vip_monthly",
                    ProductType = ProductType.Subscription,
                    ProductDetails = new List<ProductDetails>
                    {
                        new ProductDetails { Title = "VIP Monthly", Description = "Monthly VIP membership.", Language = TranslationLocale.en_US },
                    },
                    PricingDetails = new List<PricingDetails>
                    {
                        new PricingDetails { CurrencyCode = "USD", Amount = 14.99 },
                    },
                },
            };
        }
    }

    [Serializable, DataContract]
    public class ProductDetails
    {
        [CsvColumn("Title", 2)]
        [DataMember(Name = "title")]
        public string Title;
        // A blank cell keeps an empty string rather than null: a detail row always has a description
        // field, even an empty one.
        [CsvColumn("Description", 3, Fallback = "")]
        [DataMember(Name = "description")]
        public string Description;
        [CsvColumn("Language", 7,
            Fallback = TranslationLocale.en_US, Converter = typeof(CsvLocaleConverter))]
        [DataMember(Name = "language")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TranslationLocale Language;
        [CsvColumn("Subtitle", 4)]
        [DataMember(Name = "subtitle"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Subtitle;
        [CsvNested(RequiredMember = nameof(ProductBadge.Text))]
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
        [CsvColumn("BadgeText", 5)]
        [DataMember(Name = "text")]
        public string Text;
        [CsvColumn("BadgeImageUrl", 6)]
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

        [CsvColumn("CurrencyCode", 9)]
        [DataMember(Name = "currencyCode")]
        public string CurrencyCode;
        [CsvColumn("Amount", 10)]
        [DataMember(Name = "amount")]
        public double Amount;
        [CsvColumn("WebshopPrice", 11, Converter = typeof(CsvWebshopPriceConverter))]
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
        [CsvColumn("HdImageUrl", 19)]
        [DataMember(Name = "url")]
        public string Url;
        [CsvColumn("HdImageAltText", 20)]
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
        // No Fallback: a promotion with an unrecognized type is no promotion at all, and the parser
        // says which types it knows instead of quietly picking one. None says the same thing in a name
        // the enum defines, so it reads as no promotion and writes as a blank cell.
        [CsvColumn("PromotionType", 21, Absent = PromotionType.None)]
        [DataMember(Name = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PromotionType Type;
        [CsvColumn("PromotionStartsAt", 22)]
        [DataMember(Name = "startsAt"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartsAt;
        [CsvColumn("PromotionEndsAt", 23)]
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
        None,
        Sale,
        Bonus,
        Limited,
    }
}
