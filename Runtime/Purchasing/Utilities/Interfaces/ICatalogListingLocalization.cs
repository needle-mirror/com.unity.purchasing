#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing.Utilities
{
    internal interface ICatalogListingLocalization
    {
        public string? SelectLanguage(IReadOnlyCollection<string> locales, string? playerLocale);
        public string? SelectCurrency(IReadOnlyCollection<string> currencies, string? playerCurrency);
        public string CreatePriceString(decimal price, string? currencyCode, string? playerLocale);
    }

    internal class CatalogListingLocalization : ICatalogListingLocalization
    {
        readonly ICurrencyFormatter m_CurrencyFormatter;

        public CatalogListingLocalization(ICurrencyFormatter currencyFormatter)
        {
            m_CurrencyFormatter = currencyFormatter;
        }

        public string? SelectLanguage(IReadOnlyCollection<string> locales, string? playerLocale)
        {
            if (playerLocale == null || string.IsNullOrEmpty(playerLocale))
            {
                return locales.FirstOrDefault();
            }

            var normalizedPlayer = NormalizeLocale(playerLocale);
            var match = locales.FirstOrDefault(l => NormalizeLocale(l) == normalizedPlayer);
            if (match != null)
            {
                return match;
            }

            // Native locales (en-NG, zh-Hans-CN) rarely appear verbatim in catalog
            // language lists. Prefer, in order: same language and region with a
            // compatible script (zh-CN -> zh-Hans-CN), same language with a compatible
            // script (zh-Hant -> zh-Hant-TW), same language (en-NG -> en-US), then the
            // arbitrary first entry. An absent script is compatible with any script, except
            // for Chinese, whose script is implied by the region (see ImpliedScript).
            var player = Subtags(normalizedPlayer);
            return locales.FirstOrDefault(l => Matches(Subtags(NormalizeLocale(l)), player, requireRegion: true))
                ?? locales.FirstOrDefault(l => Matches(Subtags(NormalizeLocale(l)), player, requireRegion: false))
                ?? locales.FirstOrDefault(l => Subtags(NormalizeLocale(l)).Language == player.Language)
                ?? locales.FirstOrDefault();
        }

        static string NormalizeLocale(string locale)
        {
            return locale.Replace('_', '-').ToLowerInvariant();
        }

        // BCP-47 shape after NormalizeLocale: language[-script(4 alpha)][-region(2 alpha or 3 digit)].
        static (string Language, string? Script, string? Region) Subtags(string normalizedLocale)
        {
            var parts = normalizedLocale.Split('-');
            var script = parts.Length > 1 && parts[1].Length == 4 ? parts[1] : null;
            var regionIndex = script == null ? 1 : 2;
            var region = parts.Length > regionIndex && IsRegion(parts[regionIndex]) ? parts[regionIndex] : null;
            var language = LanguageKey(parts[0]);
            return (language, script ?? ImpliedScript(language, region), region);
        }

        // The language subtag compared on both sides. Standard rules only:
        // - RFC 5646 §4.5 canonicalization: a subtag the IANA Language Subtag Registry marks
        //   Deprecated is replaced by its Preferred-Value (iw -> he, in -> id, ji -> yi). The
        //   Authoring catalog codes use iw-IL (the Google Play Console convention), while
        //   devices report he through toLanguageTag() / Apple / Windows.
        // - no is the Norwegian macrolanguage; its default written form is Bokmål (nb), which
        //   is what devices report. Nynorsk (nn) is a distinct language and is not matched.
        static string LanguageKey(string language)
        {
            switch (language)
            {
                case "iw":
                    return "he";
                case "in":
                    return "id";
                case "ji":
                    return "yi";
                case "no":
                    return "nb";
                default:
                    return language;
            }
        }

        // Chinese codes without a script carry it in the region, as Google Play Console and
        // Authoring catalog codes do (zh-CN, zh-TW, zh-HK): Taiwan, Hong Kong and Macau write
        // Traditional, everywhere else (and a bare zh) Simplified. Without this, a script-less
        // entry is "compatible with any script" and zh-CN could be served to a Traditional reader.
        static string? ImpliedScript(string language, string? region)
        {
            if (language != "zh")
            {
                return null;
            }
            return region == "tw" || region == "hk" || region == "mo" ? "hant" : "hans";
        }

        // ISO 3166-1 alpha-2 or UN M.49 numeric ("419" = Latin America). Mirrors
        // NativeDeviceLocale.IsRegion, which lives in another assembly.
        static bool IsRegion(string subtag)
        {
            return subtag.Length == 2
                ? char.IsLetter(subtag[0]) && char.IsLetter(subtag[1])
                : subtag.Length == 3 && char.IsDigit(subtag[0]) && char.IsDigit(subtag[1]) && char.IsDigit(subtag[2]);
        }

        static bool Matches(
            (string Language, string? Script, string? Region) candidate,
            (string Language, string? Script, string? Region) player,
            bool requireRegion)
        {
            if (candidate.Language != player.Language)
            {
                return false;
            }
            if (candidate.Script != null && player.Script != null && candidate.Script != player.Script)
            {
                return false;
            }
            return !requireRegion || (candidate.Region != null && candidate.Region == player.Region);
        }

        public string? SelectCurrency(IReadOnlyCollection<string> currencies, string? playerCurrency)
        {
            if (playerCurrency == null || string.IsNullOrEmpty(playerCurrency))
            {
                return currencies.FirstOrDefault();
            }

            var match = currencies.FirstOrDefault(c => string.Equals(c, playerCurrency, System.StringComparison.OrdinalIgnoreCase));
            return match ?? currencies.FirstOrDefault();
        }

        public string CreatePriceString(decimal price, string? currencyCode, string? playerLocale)
        {
            if (currencyCode == null || string.IsNullOrEmpty(currencyCode))
            {
                return price.ToString();
            }
            // playerLocale (CheckoutLocation.Locale) already exhausts every fallback,
            // ending on CultureInfo.CurrentCulture.Name — no further tier exists here.
            if (playerLocale == null || string.IsNullOrEmpty(playerLocale))
            {
                return price.ToString();
            }
            return m_CurrencyFormatter.Format(price, currencyCode, playerLocale.Replace('_', '-'));
        }
    }
}
