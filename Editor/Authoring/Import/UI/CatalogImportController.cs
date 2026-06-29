using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Handles all business logic for the Catalog Import window.
    /// Reads and mutates <see cref="CatalogImportState"/>, then calls
    /// <see cref="CatalogImportState.NotifyChanged"/> so views can refresh.
    /// </summary>
    class CatalogImportController : IDisposable
    {
        const string k_CatalogFileExtension = ".ucat";

        readonly CatalogImportState m_State;
        readonly IReadOnlyList<CatalogImportProvider> m_Providers;
        readonly ICatalogCsvParser m_CsvParser;

        CatalogImportProvider m_Provider;

        ICatalogFetcher Fetcher => m_Provider?.Fetcher;

        public int SelectedTab { get; private set; }

        public CatalogImportProvider ActiveProvider => m_Provider;

        /// <summary>The full ordered list of providers (one per tab).</summary>
        public IReadOnlyList<CatalogImportProvider> Providers => m_Providers;

        /// <summary>Raised after a tab switch has been confirmed and applied.</summary>
        public event Action<int> TabSwitched;

        bool HasActiveImportData =>
            m_State.DataFetched ||
            m_State.FetchedEntries.Count > 0;

        public CatalogImportController(
            CatalogImportState state,
            IReadOnlyList<CatalogImportProvider> providers,
            ICatalogCsvParser csvParser)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Providers = providers ?? throw new ArgumentNullException(nameof(providers));
            m_CsvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
        }

        /// <summary>
        /// Applies the initial provider and notifies state.
        /// Call this after constructing the controller.
        /// </summary>
        public void Initialize(int initialTab = 0)
        {
            SelectedTab = Mathf.Clamp(initialTab, 0, m_Providers.Count - 1);
            ApplyProvider(m_Providers[SelectedTab]);
        }

        internal void TrySwitchTab(int tabIndex)
        {
            if (tabIndex == SelectedTab)
                return;

            if (HasActiveImportData)
            {
                var proceed = EditorUtility.DisplayDialog(
                    "Switch Provider",
                    "Switching tabs will clear the current import data.\n\nAre you sure?",
                    "Switch",
                    "Cancel");

                if (!proceed)
                    return;
            }

            SelectedTab = tabIndex;
            ApplyProvider(m_Providers[tabIndex]);
            TabSwitched?.Invoke(tabIndex);
        }

        void ApplyProvider(CatalogImportProvider provider)
        {
            m_Provider = provider;
            m_State.Reset();
            m_State.NotifyChanged();
        }

        internal async void OnFetchClicked()
        {
            if (m_State.IsFetching)
                return;

            m_State.FetchedEntries.Clear();
            m_State.NewEntries.Clear();
            m_State.ModifiedEntries.Clear();
            m_State.UnmodifiedEntries.Clear();
            m_State.ChangedFields.Clear();

            m_State.IsFetching = true;
            m_State.StatusMessage = "Fetching catalog...";
            m_State.NotifyChanged();

            try
            {
                m_State.FetchedEntries.AddRange(await Fetcher.FetchCatalogEntries());

                CollectExistingCatalogItems();
                ClassifyEntries();

                m_State.DataFetched = true;
                UpdateClassificationMessage();
            }
            catch (Exception ex)
            {
                m_State.DataFetched = false;
                m_State.StatusMessage = ex.Message;
            }
            finally
            {
                m_State.IsFetching = false;
            }

            m_State.NotifyChanged();
        }

        internal void OnOutputFolderChanged()
        {
            if (!m_State.DataFetched)
            {
                return;
            }

            ReclassifyForOutputFolder();
        }

        internal void OnConfirmClicked()
        {
            GenerateCatalogAssets();
            m_State.NotifyChanged();
        }

        internal void OnExportCsvClicked()
        {
            var path = EditorUtility.SaveFilePanel("Export Catalog as CSV", "Assets", "Catalog", "");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!path.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(0, path.Length - 4) + Constants.CsvFileExtension;
                }
                else
                {
                    path += Constants.CsvFileExtension;
                }
            }

            try
            {
                ExportCsv(path);
                var relativePath = FileUtil.GetProjectRelativePath(path);
                if (!string.IsNullOrEmpty(relativePath))
                {
                    AssetDatabase.ImportAsset(relativePath);
                }
                m_State.StatusMessage = $"Exported {m_State.FetchedEntries.Count} item(s) to {Path.GetFileName(path)}.";
            }
            catch (Exception ex)
            {
                m_State.StatusMessage = $"CSV export failed: {ex.Message}";
            }

            m_State.NotifyChanged();
        }

        void ExportCsv(string filePath)
        {
            var skuOrder = new List<string>();
            var grouped = new Dictionary<string, List<ImportedCatalogEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in m_State.FetchedEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.Sku))
                {
                    continue;
                }

                if (!grouped.TryGetValue(entry.Sku, out var list))
                {
                    list = new List<ImportedCatalogEntry>();
                    grouped[entry.Sku] = list;
                    skuOrder.Add(entry.Sku);
                }

                list.Add(entry);
            }

            var catalogItems = new List<CatalogItem>(skuOrder.Count);
            foreach (var sku in skuOrder)
            {
                catalogItems.Add(BuildCatalogItem(grouped[sku]));
            }

            File.WriteAllText(filePath, m_CsvParser.Serialize(catalogItems));
        }

        void CollectExistingCatalogItems()
        {
            m_State.ExistingCatalogItems.Clear();
            m_State.ExistingAssetPaths.Clear();

            var outputPrefix = GetOutputFolderPrefix();
            var purchasingDeploymentProvider = PurchasingAuthoringServiceProvider.GetService<DeploymentProvider>();

            foreach (var deploymentItem in purchasingDeploymentProvider.DeploymentItems)
            {
                if (deploymentItem is not CatalogEntryDeploymentItem catalogDeploymentItem)
                    continue;

                var sku = catalogDeploymentItem.CatalogItem?.uSku;
                if (string.IsNullOrEmpty(sku))
                    continue;

                if (!catalogDeploymentItem.Path.StartsWith(outputPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var remainingPath = catalogDeploymentItem.Path.Substring(outputPrefix.Length);
                if (remainingPath.Contains('/'))
                    continue;

                if (!m_State.ExistingCatalogItems.ContainsKey(sku))
                {
                    m_State.ExistingCatalogItems[sku] = catalogDeploymentItem.CatalogItem;
                    m_State.ExistingAssetPaths[sku] = catalogDeploymentItem.Path;
                }
            }
        }

        void ClassifyEntries()
        {
            m_State.NewEntries.Clear();
            m_State.ModifiedEntries.Clear();
            m_State.UnmodifiedEntries.Clear();
            m_State.ChangedFields.Clear();

            foreach (var entry in m_State.FetchedEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.Sku))
                {
                    continue;
                }

                if (!m_State.ExistingCatalogItems.TryGetValue(entry.Sku, out var existingItem))
                {
                    m_State.NewEntries.Add(entry);
                    continue;
                }

                var diffs = FindChangedFields(entry, existingItem);

                if (diffs.Count > 0)
                {
                    m_State.ModifiedEntries.Add(entry);
                    m_State.ChangedFields[entry] = diffs;
                }
                else
                {
                    m_State.UnmodifiedEntries.Add(entry);
                }
            }
        }

        static HashSet<string> FindChangedFields(ImportedCatalogEntry entry, CatalogItem existingItem)
        {
            var diffs = new HashSet<string>(StringComparer.Ordinal);

            var fetchedTitle = string.IsNullOrWhiteSpace(entry.Title) ? entry.Sku : entry.Title;
            var fetchedDescription = entry.Description ?? string.Empty;

            var fetchedLanguage = TranslationLocale.en_US;
            if (!string.IsNullOrWhiteSpace(entry.Language))
            {
                Enum.TryParse(entry.Language, true, out fetchedLanguage);
            }

            var existingTitle = string.Empty;
            var existingDescription = string.Empty;

            if (existingItem?.ProductDetails != null)
            {
                var matchingDetail = FindDetailByLanguage(existingItem.ProductDetails, fetchedLanguage);
                if (matchingDetail != null)
                {
                    existingTitle = matchingDetail.Title ?? string.Empty;
                    existingDescription = matchingDetail.Description ?? string.Empty;
                }
            }

            if (!string.Equals(fetchedTitle, existingTitle, StringComparison.Ordinal))
            {
                diffs.Add(nameof(ImportedCatalogEntry.Title));
            }

            if (!string.Equals(fetchedDescription, existingDescription, StringComparison.Ordinal))
            {
                diffs.Add(nameof(ImportedCatalogEntry.Description));
            }

            if (!string.IsNullOrWhiteSpace(entry.CurrencyCode))
            {
                if (existingItem?.PricingDetails == null || existingItem.PricingDetails.Count == 0)
                {
                    diffs.Add("Price");
                }
                else
                {
                    var matchingPricing = FindPricingByCurrency(existingItem.PricingDetails, entry.CurrencyCode);
                    if (matchingPricing == null)
                    {
                        diffs.Add("Price");
                    }
                    else if (Math.Abs(entry.Price - matchingPricing.Amount) > 0.001)
                    {
                        diffs.Add("Price");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.ImageUrl) &&
                !string.Equals(entry.ImageUrl, existingItem?.ImageUrl ?? string.Empty, StringComparison.Ordinal))
            {
                diffs.Add(nameof(ImportedCatalogEntry.ImageUrl));
            }

            if (!string.IsNullOrWhiteSpace(entry.ProductType) && existingItem != null)
            {
                Enum.TryParse(entry.ProductType, true, out ProductType fetchedProductType);
                if (fetchedProductType != existingItem.ProductType)
                {
                    diffs.Add(nameof(ImportedCatalogEntry.ProductType));
                }
            }

            return diffs;
        }

        static ProductDetails FindDetailByLanguage(List<ProductDetails> details, TranslationLocale language)
        {
            foreach (var detail in details)
            {
                if (detail.Language == language)
                {
                    return detail;
                }
            }

            return null;
        }

        static PricingDetails FindPricingByCurrency(List<PricingDetails> pricingDetails, string currencyCode)
        {
            foreach (var pricing in pricingDetails)
            {
                if (string.Equals(pricing.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                {
                    return pricing;
                }
            }

            return null;
        }

        void GenerateCatalogAssets()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                m_State.StatusMessage = "Could not resolve Unity project root.";
                return;
            }

            var relativePath = GetOutputFolderPrefix().TrimEnd('/');
            var outputDirectory = Path.Combine(projectRoot, relativePath);
            Directory.CreateDirectory(outputDirectory);

            var activeSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in m_State.NewEntries)
            {
                activeSkus.Add(entry.Sku);
            }
            foreach (var entry in m_State.ModifiedEntries)
            {
                activeSkus.Add(entry.Sku);
            }

            var grouped = new Dictionary<string, List<ImportedCatalogEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in m_State.FetchedEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.Sku) || !activeSkus.Contains(entry.Sku))
                {
                    continue;
                }

                if (!grouped.TryGetValue(entry.Sku, out var list))
                {
                    list = new List<ImportedCatalogEntry>();
                    grouped[entry.Sku] = list;
                }

                list.Add(entry);
            }

            var createdCount = 0;
            var updatedCount = 0;

            foreach (var (sku, entries) in grouped)
            {
                var catalogItem = BuildCatalogItem(entries);
                var serializedContent = JsonConvert.SerializeObject(catalogItem, EditorCatalogItem.GetSerializationSettings());

                if (m_State.ExistingAssetPaths.TryGetValue(sku, out var existingPath))
                {
                    var fullExistingPath = Path.Combine(projectRoot, existingPath);
                    File.WriteAllText(fullExistingPath, serializedContent);
                    updatedCount++;
                }
                else
                {
                    var fileName = SanitizeFileName(sku) + k_CatalogFileExtension;
                    var filePath = Path.Combine(outputDirectory, fileName);
                    File.WriteAllText(filePath, serializedContent);
                    createdCount++;
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            m_State.Reset();
            m_State.StatusMessage = $"Created {createdCount} new, updated {updatedCount} existing catalog file(s) in {relativePath}.";
        }

        internal static CatalogItem BuildCatalogItem(List<ImportedCatalogEntry> entries)
        {
            var first = entries[0];

            var productType = ProductType.Consumable;
            if (!string.IsNullOrWhiteSpace(first.ProductType))
            {
                Enum.TryParse(first.ProductType, true, out productType);
            }

            var catalogItem = new CatalogItem
            {
                uSku = first.Sku,
                ProductType = productType,
                ImageUrl = first.ImageUrl,
                ProductDetails = new List<ProductDetails>(),
                PricingDetails = new List<PricingDetails>(),
            };

            foreach (var entry in entries)
            {
                var title = string.IsNullOrWhiteSpace(entry.Title) ? entry.Sku : entry.Title;

                var language = TranslationLocale.en_US;
                if (!string.IsNullOrWhiteSpace(entry.Language))
                {
                    Enum.TryParse(entry.Language, true, out language);
                }

                if (catalogItem.ProductDetails.Find(d => d.Language == language) == null)
                {
                    catalogItem.ProductDetails.Add(new ProductDetails
                    {
                        Title = title,
                        Description = entry.Description ?? string.Empty,
                        Language = language,
                    });
                }

                var currency = string.IsNullOrWhiteSpace(entry.CurrencyCode) ? "USD" : entry.CurrencyCode;
                if (catalogItem.PricingDetails.Find(p =>
                        string.Equals(p.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) == null)
                {
                    catalogItem.PricingDetails.Add(new PricingDetails
                    {
                        CurrencyCode = currency,
                        Amount = entry.Price,
                    });
                }
            }

            return catalogItem;
        }

        void ReclassifyForOutputFolder()
        {
            CollectExistingCatalogItems();
            ClassifyEntries();
            UpdateClassificationMessage();
            m_State.NotifyChanged();
        }

        void UpdateClassificationMessage()
        {
            m_State.StatusMessage =
                $"Fetched {m_State.FetchedEntries.Count} item(s) — " +
                $"{m_State.NewEntries.Count} new, {m_State.ModifiedEntries.Count} modified, " +
                $"{m_State.UnmodifiedEntries.Count} unmodified.";
        }

        string GetOutputFolderPrefix()
        {
            var folder = string.IsNullOrWhiteSpace(m_State.OutputFolder)
                ? "Assets"
                : m_State.OutputFolder.Replace('\\', '/').TrimStart('/');

            if (!folder.Equals("Assets", StringComparison.OrdinalIgnoreCase) &&
                !folder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                folder = "Assets/" + folder;
            }

            return folder.EndsWith("/") ? folder : folder + "/";
        }

        internal static string SanitizeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value;
        }

        public void Dispose()
        {
            m_Provider = null;
        }
    }
}
