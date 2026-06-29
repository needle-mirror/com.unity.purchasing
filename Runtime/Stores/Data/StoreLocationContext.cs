#nullable enable

namespace UnityEngine.Purchasing.Stores
{
    internal class StoreLocationContext : IStoreLocationContext
    {
        public string? CountryCode { get; set; }
        public string? CurrencyCode { get; set; }
    }
}
