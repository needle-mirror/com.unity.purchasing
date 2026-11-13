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
        /// True if every page was retrieved successfully. False if paging gave up after retries, or stopped
        /// because the server returned a cursor it had already returned.
        /// </summary>
        public bool CompletedSuccessfully;

        /// <summary>
        /// When <see cref="CompletedSuccessfully"/> is false, the cursor passed to the last request made: the
        /// one that failed, or the one whose response repeated an earlier cursor. Null if that was the first
        /// page. Useful for diagnostics.
        /// </summary>
        public string? LastCursor;
    }
}
