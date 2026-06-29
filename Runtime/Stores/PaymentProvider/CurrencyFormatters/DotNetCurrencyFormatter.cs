#nullable enable

using System.Globalization;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing.Stores.PaymentProviderCurrencyFormatters
{
    /// <summary>
    /// .NET fallback used on Editor and platforms without a native formatter.
    /// Renders the ISO code as the currency symbol.
    /// </summary>
    internal sealed class DotNetCurrencyFormatter : ICurrencyFormatter
    {
        public string Format(decimal price, string currencyCode, string localeTag)
        {
            // Fresh CultureInfo → writable NumberFormat; doesn't affect cached CultureInfo.
            NumberFormatInfo numberFormatInfo;
            try { numberFormatInfo = new CultureInfo(localeTag).NumberFormat; }
            catch { return price.ToString(); }

            numberFormatInfo.CurrencySymbol = currencyCode;
            return price.ToString("C", numberFormatInfo);
        }
    }
}
