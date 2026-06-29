using System;
using UnityEditor.Purchasing.Editor.Authoring.Import.UI;

namespace UnityEditor.Purchasing.Editor.Authoring.Import
{
    /// <summary>
    /// Bundles the collaborators needed for a single catalog import provider tab.
    /// This is the unit that <see cref="CatalogImportWindow"/> swaps when the user changes tabs.
    /// </summary>
    sealed class CatalogImportProvider
    {
        /// <summary>Display name shown on the tab.</summary>
        public string Name { get; }

        /// <summary>
        /// USS class name that sets a background-image for the tab icon, or null.
        /// </summary>
        public string IconCssClass { get; }

        public ICatalogFetcher Fetcher { get; }
        public IConfigDrawer FetcherConfigDrawer { get; }

        public string OutputFolder { get; set; } = CatalogImportState.k_DefaultOutputFolder;

        public CatalogImportProvider(
            string name,
            ICatalogFetcher fetcher,
            IConfigDrawer fetcherConfigDrawer,
            string iconCssClass = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));
            FetcherConfigDrawer = fetcherConfigDrawer;
            IconCssClass = iconCssClass;
        }
    }
}
