using System;
using System.Collections.Generic;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Import
{
    /// <summary>
    /// Holds all mutable data for an active catalog import session.
    /// Raise <see cref="StateChanged"/> after any mutation so that views can refresh.
    /// </summary>
    internal class CatalogImportState
    {
        internal const string k_DefaultOutputFolder = "Assets/";

        public List<ImportedCatalogEntry> FetchedEntries { get; } = new List<ImportedCatalogEntry>();
        public List<ImportedCatalogEntry> NewEntries { get; } = new List<ImportedCatalogEntry>();
        public List<ImportedCatalogEntry> ModifiedEntries { get; } = new List<ImportedCatalogEntry>();
        public List<ImportedCatalogEntry> UnmodifiedEntries { get; } = new List<ImportedCatalogEntry>();

        public Dictionary<string, CatalogItem> ExistingCatalogItems { get; } =
            new Dictionary<string, CatalogItem>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ExistingAssetPaths { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<ImportedCatalogEntry, HashSet<string>> ChangedFields { get; } =
            new Dictionary<ImportedCatalogEntry, HashSet<string>>();

        public bool DataFetched { get; internal set; }

        public bool IsFetching { get; internal set; }

        public string StatusMessage { get; internal set; } = string.Empty;

        public string FetchStatusMessage { get; internal set; } = string.Empty;

        public string OutputFolder { get; set; } = k_DefaultOutputFolder;

        /// <summary>Raised after any state mutation. Views subscribe to this to trigger a UI refresh.</summary>
        public event Action StateChanged;

        /// <summary>
        /// Clears all entry lists and resets fetch/status flags.
        /// Does not reset <see cref="OutputFolder"/>.
        /// </summary>
        public void Reset()
        {
            FetchedEntries.Clear();
            NewEntries.Clear();
            ModifiedEntries.Clear();
            UnmodifiedEntries.Clear();
            ExistingCatalogItems.Clear();
            ExistingAssetPaths.Clear();
            ChangedFields.Clear();
            DataFetched = false;
            IsFetching = false;
            StatusMessage = string.Empty;
            FetchStatusMessage = string.Empty;
        }

        /// <summary>Notifies all subscribers that the state has changed.</summary>
        internal void NotifyChanged() => StateChanged?.Invoke();
    }
}
