using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Unity.Purchasing.Editor.Shared.Assets;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.IO;

namespace UnityEditor.Purchasing.Editor.Authoring.Model
{
    sealed class ObservableCatalogCsvAssets : ObservableCollection<CatalogCsvAsset>, IDisposable
    {
        readonly ICatalogCsvParser m_Parser;
        readonly AssetPostprocessorProxy m_Postprocessor;
        readonly Dictionary<string, CatalogCsvDeploymentItem> m_FileItems = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, List<CatalogEntryDeploymentItem>> m_EntryItems = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<IDeploymentItem> DeploymentItems { get; } = new();

        internal ObservableAssets<CatalogCsvAsset> CsvAssetCollection { get; }

        public ObservableCatalogCsvAssets(ICatalogCsvParser parser)
            : this(parser, new AssetPostprocessorProxy(), true)
        {
        }

        internal ObservableCatalogCsvAssets(
            ICatalogCsvParser parser,
            AssetPostprocessorProxy postprocessor,
            bool loadAssets)
        {
            m_Parser = parser;
            m_Postprocessor = postprocessor;

            var assetCollectionProxy = loadAssets ? postprocessor : new AssetPostprocessorProxy();
            CsvAssetCollection = new ObservableAssets<CatalogCsvAsset>(
                new[] { Constants.CsvFileExtension }, assetCollectionProxy, loadAssets);

            foreach (var asset in CsvAssetCollection)
            {
                Add(asset);
                AddOrUpdate(asset.Path);
            }

            CsvAssetCollection.CollectionChanged += OnCollectionChanged;
            m_Postprocessor.AllAssetsPostprocessed += OnAllAssetsPostprocessed;
            CatalogUserSettings.SettingChanged += OnToggleChanged;
        }

        public void Dispose()
        {
            CatalogUserSettings.SettingChanged -= OnToggleChanged;
            CsvAssetCollection.CollectionChanged -= OnCollectionChanged;
            m_Postprocessor.AllAssetsPostprocessed -= OnAllAssetsPostprocessed;
            CsvAssetCollection.Dispose();
        }

        public CatalogCsvDeploymentItem GetDeploymentItem(string path)
        {
            m_FileItems.TryGetValue(path, out var item);
            return item;
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CatalogCsvAsset asset in e.NewItems)
                {
                    Add(asset);
                    AddOrUpdate(asset.Path);
                }
            }

            if (e.OldItems != null)
            {
                foreach (CatalogCsvAsset asset in e.OldItems)
                {
                    Remove(asset);
                    RemoveAsset(asset.Path);
                }
            }
        }

        void OnAllAssetsPostprocessed(object sender, PostProcessEventArgs args)
        {
            foreach (var path in args.ImportedAssetPaths)
            {
                if (!path.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (m_FileItems.TryGetValue(path, out var item))
                {
                    Repopulate(item, path);
                }
            }

            for (var i = 0; i < args.MovedAssetPaths.Length; i++)
            {
                var toPath = args.MovedAssetPaths[i];
                var fromPath = args.MovedFromAssetPaths[i];

                if (!fromPath.EndsWith(Constants.CsvFileExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!m_FileItems.TryGetValue(fromPath, out var item))
                    continue;

                m_FileItems.Remove(fromPath);
                item.Path = toPath;
                item.Name = System.IO.Path.GetFileName(toPath);
                m_FileItems[toPath] = item;

                if (m_EntryItems.TryGetValue(fromPath, out var entries))
                {
                    m_EntryItems.Remove(fromPath);
                    foreach (var entry in entries)
                    {
                        entry.Path = toPath;
                    }
                    m_EntryItems[toPath] = entries;
                }

                CatalogUserSettings.TransferPath(fromPath, toPath);
            }
        }

        void RemoveAsset(string path)
        {
            if (m_FileItems.TryGetValue(path, out var item))
            {
                RemoveCsv(item);
                m_FileItems.Remove(path);
            }

            m_EntryItems.Remove(path);
            CatalogUserSettings.SetShownAsCsv(path, true);
        }

        void AddOrUpdate(string path)
        {
            if (m_FileItems.TryGetValue(path, out var existing))
            {
                Repopulate(existing, path);
                return;
            }

            var item = new CatalogCsvDeploymentItem(path);
            ParseInto(item, path);
            m_FileItems[path] = item;
            AddCsv(item);
        }

        void AddCsv(CatalogCsvDeploymentItem csv)
        {
            // Fallback to showing the container when expanded view has no entries
            // (parse failure or empty CSV) so the error State stays visible.
            var hasEntries = csv.EntryDeploymentItems.Count > 0;
            if (CatalogUserSettings.IsShownAsCsv(csv.Path) || !hasEntries)
            {
                DeploymentItems.Add(csv);
            }
            else
            {
                foreach (var entry in csv.EntryDeploymentItems)
                {
                    DeploymentItems.Add(entry);
                }
            }
        }

        void RemoveCsv(CatalogCsvDeploymentItem csv)
        {
            DeploymentItems.Remove(csv);
            foreach (var entry in csv.EntryDeploymentItems)
            {
                DeploymentItems.Remove(entry);
            }
        }

        void OnToggleChanged()
        {
            foreach (var csv in m_FileItems.Values)
            {
                ReconcileCsv(csv);
            }
        }

        void ReconcileCsv(CatalogCsvDeploymentItem csv)
        {
            // Fallback to showing the container when expanded view has no entries
            // (parse failure or empty CSV) so the error State stays visible.
            var hasEntries = csv.EntryDeploymentItems.Count > 0;
            var shouldShowCsv = CatalogUserSettings.IsShownAsCsv(csv.Path) || !hasEntries;
            var isShowingCsv = DeploymentItems.Contains(csv);
            if (shouldShowCsv == isShowingCsv)
                return;

            if (shouldShowCsv)
            {
                foreach (var entry in csv.EntryDeploymentItems)
                {
                    DeploymentItems.Remove(entry);
                }
                DeploymentItems.Add(csv);
            }
            else
            {
                DeploymentItems.Remove(csv);
                foreach (var entry in csv.EntryDeploymentItems)
                {
                    DeploymentItems.Add(entry);
                }
            }
        }

        void Repopulate(CatalogCsvDeploymentItem item, string path)
        {
            var oldEntries = item.EntryDeploymentItems ?? new List<CatalogEntryDeploymentItem>();
            ParseInto(item, path);

            // csv-view: the CatalogCsvDeploymentItem reference is unchanged so no DeploymentItems swap is needed.
            if (CatalogUserSettings.IsShownAsCsv(path))
                return;

            // expanded-view: tear down whichever representation was previously shown
            // (entries on success, or the container itself if a prior parse had failed)
            // then rebuild via AddCsv so the empty-entries fallback applies.
            foreach (var entry in oldEntries)
            {
                DeploymentItems.Remove(entry);
            }
            DeploymentItems.Remove(item);

            if (item.EntryDeploymentItems.Count > 0)
            {
                foreach (var entry in item.EntryDeploymentItems)
                {
                    DeploymentItems.Add(entry);
                }
            }
            else
            {
                DeploymentItems.Add(item);
            }
        }

        void ParseInto(CatalogCsvDeploymentItem item, string path)
        {
            item.ClearTypedStates(CatalogCsvParser.ParseStateType);
            try
            {
                var fullPath = Path.GetFullPath(path);
                var content = File.ReadAllText(fullPath);
                var catalogItems = m_Parser.Parse(content, out var parseIssues);
                item.CatalogItems = catalogItems;
                item.EntryDeploymentItems = BuildEntries(path, catalogItems);
                m_EntryItems[path] = item.EntryDeploymentItems;

                foreach (var issue in parseIssues)
                    item.States.Add(issue);

                item.Validate();
            }
            catch (Exception ex)
            {
                item.CatalogItems = new List<CatalogItem>();
                item.EntryDeploymentItems = new List<CatalogEntryDeploymentItem>();
                m_EntryItems.Remove(path);
                item.States.Add(new AssetState(
                    "Failed to parse CSV", ex.Message,
                    SeverityLevel.Error, CatalogCsvParser.ParseStateType));
            }
        }

        static List<CatalogEntryDeploymentItem> BuildEntries(string csvPath, List<CatalogItem> catalogItems)
        {
            var entries = new List<CatalogEntryDeploymentItem>(catalogItems.Count);
            foreach (var catalogItem in catalogItems)
            {
                entries.Add(new CatalogEntryDeploymentItem(csvPath)
                {
                    CatalogItem = catalogItem,
                    Name = catalogItem.uSku + Constants.FileExtension,
                });
            }
            return entries;
        }
    }
}
