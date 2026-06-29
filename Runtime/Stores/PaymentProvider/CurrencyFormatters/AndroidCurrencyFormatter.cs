#nullable enable

using UnityEngine.Purchasing.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Stores.PaymentProviderCurrencyFormatters
{
    /// <summary>
    /// Android currency formatter backed by <c>java.text.NumberFormat</c>.
    /// </summary>
    internal sealed class AndroidCurrencyFormatter : ICurrencyFormatter
    {
        readonly ICurrencyFormatter m_Fallback;

        [Preserve]
        [Inject]
        public AndroidCurrencyFormatter() : this(new DotNetCurrencyFormatter()) { }

        // Test seam.
        public AndroidCurrencyFormatter(ICurrencyFormatter fallback)
        {
            m_Fallback = fallback;
        }

        public string Format(decimal price, string currencyCode, string localeTag)
        {
            return TryFormatNative(price, currencyCode, localeTag)
                   ?? m_Fallback.Format(price, currencyCode, localeTag);
        }

        static string? TryFormatNative(decimal price, string iso, string localeTag)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var localeClass = new AndroidJavaClass("java.util.Locale");
                using var locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", localeTag);
                using var numberFormatClass = new AndroidJavaClass("java.text.NumberFormat");
                using var numberFormat = numberFormatClass.CallStatic<AndroidJavaObject>("getCurrencyInstance", locale);
                using var currencyClass = new AndroidJavaClass("java.util.Currency");
                using var currency = currencyClass.CallStatic<AndroidJavaObject>("getInstance", iso);

                numberFormat.Call("setCurrency", currency);
                var digits = currency.Call<int>("getDefaultFractionDigits");
                numberFormat.Call("setMinimumFractionDigits", digits);
                numberFormat.Call("setMaximumFractionDigits", digits);

                return numberFormat.Call<string>("format", (double)price);
            }
            catch
            {
                return null;
            }
#else
            return null;
#endif
        }
    }
}
