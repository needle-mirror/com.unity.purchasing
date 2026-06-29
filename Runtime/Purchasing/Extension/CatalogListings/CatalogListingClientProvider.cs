#nullable enable
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.CatalogListings
{
    internal static class CatalogListingClientProvider
    {
        static ICatalogListingClient? s_Instance;

        public static ICatalogListingClient Instance()
        {
            return s_Instance ??= new CatalogListingClient(
                LiveContentAdapterServiceProvider.Instance(),
                new CatalogListingParser(),
                UnityUtilContainer.Instance()
            );
        }
    }
}
