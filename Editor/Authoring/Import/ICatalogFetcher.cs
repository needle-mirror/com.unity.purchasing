using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Import
{
    /// <summary>
    /// Abstracts the catalog fetching logic for a specific store or data source.
    /// Implementations handle provider-specific API calls, authentication, and response parsing.
    /// </summary>
    interface ICatalogFetcher
    {
        /// <summary>
        /// Fetch catalog entries from the external source.
        /// </summary>
        /// <returns>A list of imported catalog entries.</returns>
        Task<List<ImportedCatalogEntry>> FetchCatalogEntries();
    }
}
