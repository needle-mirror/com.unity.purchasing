#nullable enable

namespace UnityEngine.Purchasing.Utilities
{
    internal interface ICurrencyFormatter
    {
        string Format(decimal price, string currencyCode, string localeTag);
    }
}
