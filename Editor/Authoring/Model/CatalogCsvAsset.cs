using System.Collections.Generic;
using Unity.Purchasing.Editor.Shared.Assets;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.IO;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    class CatalogCsvAsset : ScriptableObject, IPath
    {
        internal const string DefaultFileName = "MyCatalog";
        static readonly CatalogCsvParser s_Parser = new();

        public string Path { get; set; }

        internal static string GenerateDefaultContent()
        {
            var items = new List<CatalogItem>
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
            return s_Parser.Serialize(items);
        }

        [MenuItem("Assets/Create/Services/IAP Catalog CSV", false, 82)]
        public static void CreateCatalogCsv()
        {
            var folder = CatalogAssetHelper.GetActiveFolderPath();
            var path = CatalogAssetHelper.GenerateUniquePath(folder, DefaultFileName, Constants.CsvFileExtension);

            var endAction = CreateInstance<CreateCatalogCsvAssetAction>();
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(default(UnityEngine.EntityId), endAction, path, null, null);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, path, null, null);
#endif
        }
    }
}
