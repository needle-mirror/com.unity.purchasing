#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Purchasing.CatalogListings;
using UnityEngine.Purchasing.LiveContentAdapterService;

namespace UnityEngine.Purchasing.Utilities
{
    internal class CatalogListingParser : ICatalogListingParser
    {
        public CatalogListingDto? TryParseCatalogConfigContent(ConfigContentData contentData)
        {
            try
            {
                var dto = JsonConvert.DeserializeObject<CatalogListingDto>(contentData.content);
                if (dto != null)
                {
                    dto.CatalogListingId = contentData.path;
                    dto.HasWebshop = contentData.schemas?.Any(s  => s.Contains(CatalogListingClient.k_WebshopSchemaUrl)) ?? false;
                }
                return dto;
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPCallVerbose($"Failed to parse catalog config content with id {contentData.id}. Exception: {e}", "CatalogListingParser");
                return null;
            }
        }

        public List<CatalogListingDto> TryParseCatalogResponse(IReadOnlyCollection<ConfigContentData> contentDataCollection)
        {
            return contentDataCollection
                .Select(TryParseCatalogConfigContent)
                // filters out null values in a way the compiler understands
                .OfType<CatalogListingDto>()
                .ToList();
        }
    }
}
