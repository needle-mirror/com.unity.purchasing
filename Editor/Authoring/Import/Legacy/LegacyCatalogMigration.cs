using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Import.UI;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.Legacy
{
    /// <summary>
    /// Converts a codeless <c>IAPProductCatalog.json</c> into a catalog CSV the Remote Catalog can
    /// deploy. Reuses the store-import pipeline: the legacy items become
    /// <see cref="ImportedCatalogEntry"/> rows and <c>CatalogImportController.BuildCatalogItem</c>
    /// turns them into catalog items, so both paths produce identical output.
    /// </summary>
    static class LegacyCatalogMigration
    {
        internal const string GeneratedCatalogName = "MigratedCatalog";

        /// <summary>The legacy catalog records no currency, so one has to be chosen.</summary>
        internal const string AssumedCurrency = "USD";

        /// <summary>Whether this project has a legacy catalog worth migrating.</summary>
        internal static bool HasLegacyCatalog()
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();

            // Matches what the catalog window treats as empty: entries without an id migrate to
            // nothing. Null entries are legal in a catalog, hence the null-conditional.
            return catalog != null && catalog.allProducts.Any(product => !string.IsNullOrEmpty(product?.id));
        }

        /// <summary>
        /// Writes the legacy catalog out as a catalog CSV asset.
        /// </summary>
        /// <param name="folder">Project folder to write into. Defaults to the active folder.</param>
        /// <returns>The asset path written, or null when there is nothing to migrate.</returns>
        internal static string Migrate(string folder = null)
        {
            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog == null || catalog.allProducts.Count == 0)
            {
                return null;
            }

            var catalogItems = new List<CatalogItem>();
            var notMigrated = new List<string>();

            foreach (var product in catalog.allProducts)
            {
                // A catalog can hold null entries; codeless skips them and so do we.
                if (product == null)
                {
                    continue;
                }

                var entries = ToEntries(product);
                if (entries.Count > 0)
                {
                    var item = CatalogImportController.BuildCatalogItem(entries);
                    item.StoreIdOverrides = ToStoreIdOverrides(product, notMigrated);
                    catalogItems.Add(item);
                }
            }

            if (catalogItems.Count == 0)
            {
                return null;
            }

            folder ??= CatalogAssetHelper.GetActiveFolderPath();
            var path = CatalogAssetHelper.GenerateUniquePath(folder, GeneratedCatalogName, Constants.CsvFileExtension);

            File.WriteAllText(path, new CatalogCsvParser().Serialize(catalogItems));
            AssetDatabase.Refresh();

            WarnAboutItemsThatWillNotDeploy(catalogItems);
            WarnAboutFieldsNotMigrated(catalog, notMigrated);

            return path;
        }

        /// <summary>
        /// Legacy limits are looser than the Remote Catalog's: Google titles may run to 55
        /// characters against a limit of 50, and legacy ids are only checked for blanks and
        /// duplicates. Anything that would be rejected on deploy is named here, so migration does
        /// not report success on a file that cannot be deployed.
        /// </summary>
        static void WarnAboutItemsThatWillNotDeploy(List<CatalogItem> items)
        {
            var problems = new List<string>();

            foreach (var item in items)
            {
                foreach (var state in item.Validate())
                {
                    problems.Add($"{item.uSku}: {state.Description}");
                }
            }

            if (problems.Count == 0)
            {
                return;
            }

            Debug.unityLogger.LogWarning("InAppPurchasing",
                "The migrated catalog has items the Remote Catalog will reject on deploy. "
                + "Fix them in the generated file:\n  " + string.Join("\n  ", problems));
        }

        /// <summary>
        /// The Remote Catalog has no equivalent for several legacy fields. Anything that carried a
        /// value is named here rather than dropped silently.
        /// </summary>
        static void WarnAboutFieldsNotMigrated(ProductCatalog catalog, List<string> notMigrated)
        {
            var dropped = new List<string>(notMigrated);

            foreach (var product in catalog.allProducts)
            {
                if (product == null)
                {
                    continue;
                }

                if (product.Payouts.Count > 0)
                {
                    dropped.Add($"{product.id}: payouts ({product.Payouts.Count})");
                }

                if (!string.IsNullOrEmpty(product.screenshotPath))
                {
                    dropped.Add($"{product.id}: screenshot path");
                }

                // The Apple exporter prices by tier and needs no Google price, so an Apple-only
                // product would otherwise migrate as a silent zero.
                if (product.googlePrice.value == 0 && product.applePriceTier != 0)
                {
                    dropped.Add($"{product.id}: priced by Apple price tier only, migrated with no price");
                }

                if (!string.IsNullOrEmpty(product.pricingTemplateID))
                {
                    dropped.Add($"{product.id}: pricing template id");
                }
            }

            // The legacy catalog stores a bare number: the Google export writes it as micro-units
            // and the Apple export uses a price tier, so neither records a currency.
            if (catalog.allProducts.Any(product => product != null && product.googlePrice.value != 0))
            {
                dropped.Add($"all priced products: currency assumed to be {AssumedCurrency}, which the legacy catalog does not record");
            }

            if (dropped.Count == 0)
            {
                return;
            }

            // LoggerExtensions.LogIAPWarning is internal to Unity.Purchasing.Utilities; this is the
            // same call it makes, with the same tag.
            Debug.unityLogger.LogWarning("InAppPurchasing",
                "These legacy catalog fields have no Remote Catalog equivalent and were not migrated. "
                + "The original file still holds them:\n  " + string.Join("\n  ", dropped));
        }

        /// <summary>
        /// Legacy per-store product ids map directly onto the Remote Catalog's store overrides.
        /// The legacy catalog has no Xbox entry, so that store never appears here.
        /// </summary>
        internal static List<StoreIdOverride> ToStoreIdOverrides(ProductCatalogItem product, List<string> notMigrated)
        {
            var overrides = new List<StoreIdOverride>();

            foreach (var storeId in product.allStoreIDs)
            {
                if (string.IsNullOrWhiteSpace(storeId?.id))
                {
                    continue;
                }

                StoreId store;
                switch (storeId.store)
                {
                    case GooglePlay.Name: store = StoreId.Google; break;
                    case AppleAppStore.Name: store = StoreId.Apple; break;
                    case MacAppStore.Name: store = StoreId.MacAppStore; break;
                    default:
                        // Legacy catalogs carry stores the Remote Catalog has no slot for, WinRT
                        // and UDP among them. Say so rather than dropping the id.
                        notMigrated.Add($"{product.id}: store id override for {storeId.store}");
                        continue;
                }

                overrides.Add(new StoreIdOverride { Store = store, Value = storeId.id });
            }

            return overrides;
        }

        /// <summary>One entry per locale, which is the shape the store fetchers produce.</summary>
        internal static List<ImportedCatalogEntry> ToEntries(ProductCatalogItem product)
        {
            var entries = new List<ImportedCatalogEntry>();
            if (string.IsNullOrWhiteSpace(product?.id))
            {
                return entries;
            }

            const string currency = AssumedCurrency;
            var price = (double)product.googlePrice.value;

            entries.Add(NewEntry(product, product.defaultDescription, currency, price));

            foreach (var description in product.translatedDescriptions)
            {
                entries.Add(NewEntry(product, description, currency, price));
            }

            return entries;
        }

        static ImportedCatalogEntry NewEntry(ProductCatalogItem product, LocalizedProductDescription description,
            string currency, double price)
        {
            return new ImportedCatalogEntry
            {
                Sku = product.id,
                Title = description?.Title,
                Description = description?.Description,
                Language = description?.googleLocale.ToString(),
                ProductType = product.type.ToString(),
                CurrencyCode = currency,
                Price = price
            };
        }
    }
}
