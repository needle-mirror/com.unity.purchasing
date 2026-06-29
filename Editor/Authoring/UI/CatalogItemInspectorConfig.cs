using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Model.Extensions;
using UnityEngine;
using UnityEngine.Purchasing;
using StoreId = UnityEditor.Purchasing.Editor.Authoring.Core.StoreId;

namespace UnityEditor.Purchasing.Authoring
{
    [Serializable]
    class CatalogItemInspectorConfig : ScriptableObject
    {
        [Tooltip("Identifier derived from the file name. Rename the asset to change.")]
        [CustomReadOnly]
        public string CatalogListingId;
        [Tooltip("SKU for the item. Defaults to the file name; edit to override.")]
        public string Sku;
        [Tooltip("The product type for this item")]
        public ProductType ProductType;
        [Tooltip("The details for this item")]
        public List<ProductDetails> ProductDetails;
        [Tooltip("The pricing details for this item")]
        public List<PricingDetails> PricingDetails;
        [Tooltip("The image URL for this product.")]
        public string ImageUrl;

        [Header("Webshop availability")]
        [Tooltip("Sell this SKU on Webshop too. When on, Categories, HD Images, and Promotion are deployed and the Webshop schema is added; when off, they are stripped from the upsert.")]
        public bool IsWebshopAvailable;
        [Tooltip("Category IDs this product belongs to on the Webshop.")]
        public List<string> Categories;
        [Tooltip("High-resolution images for the Webshop product detail dialog.")]
        public List<HdImage> HdImages;
        [Tooltip("Webshop promotion treatment (Sale / Bonus / Limited) and optional active window.")]
        public Promotion Promotion;

        [Header("Store ID Overrides")]
        [Tooltip("Apple App Store product ID for this catalog item. Leave empty to use the SKU.")]
        public string AppleOverride;
        [Tooltip("Google Play product ID for this catalog item. Leave empty to use the SKU.")]
        public string GoogleOverride;
        [Tooltip("Xbox Store product ID for this catalog item. Leave empty to use the SKU.")]
        public string XboxStoreOverride;
        [Tooltip("Mac App Store product ID for this catalog item. Leave empty to use the SKU.")]
        public string MacAppStoreOverride;

        public void Initialize(CatalogItem catalogItem)
        {
            CatalogListingId = catalogItem.CatalogListingId;
            Sku = catalogItem.uSku;
            ProductType = catalogItem.ProductType.ConvertProductType();
            ProductDetails = catalogItem.ProductDetails?.Select(d => new ProductDetails(d)).ToList() ?? new List<ProductDetails>();
            PricingDetails = catalogItem.PricingDetails?.Select(d => new PricingDetails(d)).ToList() ?? new List<PricingDetails>();
            ImageUrl = catalogItem.ImageUrl;
            IsWebshopAvailable = catalogItem.IsWebshopAvailable;
            Categories = catalogItem.Categories is null ? null : new List<string>(catalogItem.Categories);
            HdImages = catalogItem.HdImages?.Select(i => new HdImage(i)).ToList();
            Promotion = catalogItem.Promotion is null ? null : new Promotion(catalogItem.Promotion);
            AppleOverride = catalogItem.StoreIdOverrides?.FirstOrDefault(o => o.Store == StoreId.Apple)?.Value;
            GoogleOverride = catalogItem.StoreIdOverrides?.FirstOrDefault(o => o.Store == StoreId.Google)?.Value;
            XboxStoreOverride = catalogItem.StoreIdOverrides?.FirstOrDefault(o => o.Store == StoreId.XboxStore)?.Value;
            MacAppStoreOverride = catalogItem.StoreIdOverrides?.FirstOrDefault(o => o.Store == StoreId.MacAppStore)?.Value;
        }
    }
}
