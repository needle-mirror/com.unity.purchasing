using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Model.Extensions;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    [Serializable]
    class EditorCatalogItem : CatalogItem
    {
        // Schema does not exist yet
        // [JsonProperty("$schema")]
        // public string Schema => "https://ugs-config-schemas.unity3d.com/v1/my-service.schema.json";

        [JsonIgnore]
        public string Extension => Constants.FileExtension;

        [JsonIgnore]
        public string FileBodyText =>
            JsonConvert.SerializeObject(
                CreateDefaultCatalog(),
                GetSerializationSettings());

        public static JsonSerializerSettings GetSerializationSettings()
        {
            var settings = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Include
            };
            return settings;
        }

        public void CopyFrom(CatalogItemInspectorConfig config)
        {
            uSku = config.Sku;
            ProductType = config.ProductType.ConvertProductType();
            ProductDetails = new List<ProductDetails>(config.ProductDetails);
            PricingDetails = new List<PricingDetails>(config.PricingDetails);
            ImageUrl = config.ImageUrl;
            // Webshop fields are stored regardless of the toggle so users don't lose data when
            // they flip it off then on. The toggle gates upsert (ConvertToDto strips them when off).
            IsWebshopAvailable = config.IsWebshopAvailable;
            Categories = config.Categories?.Count > 0 ? new List<string>(config.Categories) : null;
            HdImages = BuildHdImages(config.HdImages);
            Promotion = config.Promotion is not null ? new Promotion(config.Promotion) : null;
            StoreIdOverrides = BuildOverrides(config);
        }

        static List<HdImage> BuildHdImages(List<HdImage> source)
        {
            if (source is null || source.Count == 0)
                return null;
            var list = new List<HdImage>(source.Count);
            foreach (var img in source)
                list.Add(new HdImage(img));
            return list;
        }

        static List<StoreIdOverride> BuildOverrides(CatalogItemInspectorConfig config)
        {
            var list = new List<StoreIdOverride>();
            if (!string.IsNullOrEmpty(config.AppleOverride))
                list.Add(new StoreIdOverride { Store = StoreId.Apple, Value = config.AppleOverride });
            if (!string.IsNullOrEmpty(config.GoogleOverride))
                list.Add(new StoreIdOverride { Store = StoreId.Google, Value = config.GoogleOverride });
            if (!string.IsNullOrEmpty(config.XboxStoreOverride))
                list.Add(new StoreIdOverride { Store = StoreId.XboxStore, Value = config.XboxStoreOverride });
            if (!string.IsNullOrEmpty(config.MacAppStoreOverride))
                list.Add(new StoreIdOverride { Store = StoreId.MacAppStore, Value = config.MacAppStoreOverride });
            return list.Count > 0 ? list : null;
        }
    }
}
