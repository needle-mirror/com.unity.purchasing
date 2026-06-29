#nullable enable
using System.Collections.Generic;
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.CatalogListings;

namespace UnityEngine.Purchasing.Utilities
{

    internal interface ICatalogListingParser
    {
        public CatalogListingDto? TryParseCatalogConfigContent(ConfigContentData contentData);

        public List<CatalogListingDto> TryParseCatalogResponse(IReadOnlyCollection<ConfigContentData> contentDataCollection);
    }
}
