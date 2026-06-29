namespace UnityEngine.Purchasing
{
    class CommonTransactionEventHelper
    {
        internal static string GetTransactionName(CatalogListing listing)
        {
            return string.IsNullOrEmpty(listing?.metadata?.localizedTitle) ?
                listing?.definition?.storeSpecificId :
                listing.metadata.localizedTitle;
        }
    }
}
