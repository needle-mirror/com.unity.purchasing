#nullable enable
using System.Collections.Generic;

namespace UnityEngine.Purchasing.CatalogListings
{
    /// <summary>
    /// Aggregate result of a paged catalog-listing fetch.
    /// </summary>
    internal class CatalogListingResult
    {
        /// <summary>
        /// Listings successfully retrieved. May be a partial set if paging failed mid-way.
        /// </summary>
        public List<CatalogListingDto> Results = new List<CatalogListingDto>();

        /// <summary>
        /// True if every page was retrieved successfully. False if paging gave up after retries.
        /// </summary>
        public bool CompletedSuccessfully;

        /// <summary>
        /// When <see cref="CompletedSuccessfully"/> is false, the cursor value that was passed to the
        /// request that ultimately failed (null if the first page itself failed). Useful for diagnostics.
        /// </summary>
        public string? LastFailedAfter;
    }
}
