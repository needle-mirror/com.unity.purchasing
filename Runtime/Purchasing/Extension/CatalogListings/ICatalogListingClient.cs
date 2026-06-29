#nullable enable
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.CatalogListings
{
    internal interface ICatalogListingClient
    {
        bool IsAvailable { get; }

        Task<CatalogListingResult> GetCatalogListings();
    }
}
