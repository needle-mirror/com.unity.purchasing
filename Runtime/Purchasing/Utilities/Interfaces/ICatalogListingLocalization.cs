#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UnityEngine.Purchasing.Utilities
{
    internal interface ICatalogListingLocalization
    {
        public string? SelectLanguage(IReadOnlyCollection<string> locales, string? playerLocale, RegionInfo regionInfo, CultureInfo cultureInfo);
        public string? SelectCurrency(IReadOnlyCollection<string> currencies, string? playerCurrency, RegionInfo regionInfo, CultureInfo cultureInfo);
        public string CreatePriceString(decimal price, string? currencyCode, string? playerLocale, CultureInfo cultureInfo);
    }

    internal class CatalogListingLocalization : ICatalogListingLocalization
    {
        readonly ICurrencyFormatter m_CurrencyFormatter;

        public CatalogListingLocalization(ICurrencyFormatter currencyFormatter)
        {
            m_CurrencyFormatter = currencyFormatter;
        }

        public string? SelectLanguage(IReadOnlyCollection<string> locales, string? playerLocale, RegionInfo regionInfo, CultureInfo cultureInfo)
        {
            if (playerLocale == null || string.IsNullOrEmpty(playerLocale))
            {
                return locales.FirstOrDefault();
            }

            var normalizedPlayer = NormalizeLocale(playerLocale);
            var match = locales.FirstOrDefault(l => NormalizeLocale(l) == normalizedPlayer);
            return match ?? locales.FirstOrDefault();
        }

        static string NormalizeLocale(string locale)
        {
            return locale.Replace('_', '-').ToLowerInvariant();
        }

        public string? SelectCurrency(IReadOnlyCollection<string> currencies, string? playerCurrency, RegionInfo regionInfo, CultureInfo cultureInfo)
        {
            if (playerCurrency == null || string.IsNullOrEmpty(playerCurrency))
            {
                return currencies.FirstOrDefault();
            }

            var match = currencies.FirstOrDefault(c => string.Equals(c, playerCurrency, System.StringComparison.OrdinalIgnoreCase));
            return match ?? currencies.FirstOrDefault();
        }

        public string CreatePriceString(decimal price, string? currencyCode, string? playerLocale, CultureInfo cultureInfo)
        {
            if (currencyCode == null || string.IsNullOrEmpty(currencyCode))
            {
                return price.ToString();
            }
            var localeTag = ResolveLocaleTag(playerLocale, cultureInfo);
            if (localeTag == null)
            {
                return price.ToString();
            }
            return m_CurrencyFormatter.Format(price, currencyCode, localeTag);
        }

        // Prefer the player's locale; fall back to the current culture's name.
        // Null when neither is usable.
        internal static string? ResolveLocaleTag(string? playerLocale, CultureInfo cultureInfo)
        {
            if (playerLocale != null && !string.IsNullOrEmpty(playerLocale))
            {
                return playerLocale.Replace('_', '-');
            }
            try
            {
                var name = cultureInfo.Name;
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch { return null; }
        }
    }
}
