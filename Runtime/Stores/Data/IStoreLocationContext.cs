#nullable enable

namespace UnityEngine.Purchasing.Stores
{
    internal interface IStoreLocationContext
    {
        string? CountryCode { get; set; }
        string? CurrencyCode { get; set; }
    }
}
