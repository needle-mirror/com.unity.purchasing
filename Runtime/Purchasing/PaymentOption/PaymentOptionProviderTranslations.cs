#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.MiniJSON;
using UnityEngine.Purchasing.Utilities;

namespace UnityEngine.Purchasing
{
    static class PaymentOptionProviderTranslations
    {
        internal static class Keys
        {
            // A format: {0} is the store's name, or its logo.
            internal const string Button = "pop.button";
            internal const string Header = "pop.header";
        }

        internal const string NamePlaceholder = "{0}";

        // Replace, not string.Format: a stray brace in a translation must not throw.
        internal static string ButtonLabel(string locale, string storeName)
        {
            var text = Translate(Keys.Button, locale);
            return text.Contains(NamePlaceholder)
                ? text.Replace(NamePlaceholder, storeName)
                : text.TrimEnd() + " " + storeName;
        }

        // The text before and after the logo that stands in for {0}; either may be empty.
        internal static (string Lead, string Trail) ButtonParts(string locale)
        {
            var text = Translate(Keys.Button, locale);
            var at = text.IndexOf(NamePlaceholder, StringComparison.Ordinal);
            return at < 0
                ? (text.Trim(), "")
                : (text.Substring(0, at).Trim(), text.Substring(at + NamePlaceholder.Length).Trim());
        }

        internal const string Fallback = "en-US";

        // The en-US rows, built in for when the strings file is missing or malformed.
        internal const string EnglishButton = "Continue with {0}";
        internal const string EnglishHeader = "How would you like to pay?";

        static string BuiltIn(string key) => key switch
        {
            Keys.Button => EnglishButton,
            Keys.Header => EnglishHeader,
            _ => key
        };

        const string k_ResourcePath = "com.unity.purchasing/PaymentOptionProviderStrings";

        // key -> (tag -> text). Empty after a failed load, so the load isn't retried.
        static Dictionary<string, Dictionary<string, string>>? s_Table;

        // The tags in the strings file, in file order (en-US first): what ResolveLocale matches against.
        static IReadOnlyCollection<string>? s_Locales;

        static readonly Dictionary<string, string?> s_Resolved = new(StringComparer.OrdinalIgnoreCase);

        internal static IReadOnlyCollection<string> SupportedLocales
        {
            get
            {
                EnsureTable();
                return s_Locales!;
            }
        }

        // Falls back to English, then to the built-in English, then to the key itself: a visible key
        // is easier to diagnose than a blank.
        internal static string Translate(string key, string locale)
        {
            var table = EnsureTable();
            if (!table.TryGetValue(key, out var byTag))
            {
                Debug.unityLogger.LogIAPWarning(
                    $"MissingTranslationKey: '{key}' is absent from {k_ResourcePath}; showing built-in English, else the key");
                return BuiltIn(key);
            }

            if (byTag.TryGetValue(locale, out var text) && !string.IsNullOrEmpty(text))
            {
                return text;
            }

            return byTag.TryGetValue(Fallback, out var english) && !string.IsNullOrEmpty(english)
                ? english
                : BuiltIn(key);
        }

        // The supported locale closest to a BCP-47 tag ("fr-CA" -> "fr-FR"), or null when its
        // language has no translation.
        internal static string? MatchLocale(string? localeTag)
        {
            if (string.IsNullOrEmpty(localeTag))
            {
                return null;
            }

            if (s_Resolved.TryGetValue(localeTag!, out var cached))
            {
                return cached;
            }

            var normalized = localeTag!.Replace('_', '-');
            var best = CatalogListingLocalization.SelectBestLocale(SupportedLocales, normalized);
            // The matcher's last tier returns any entry; only a same-language match counts.
            var resolved = best != null && CatalogListingLocalization.SameLanguage(best, normalized) ? best : null;
            s_Resolved[localeTag!] = resolved;
            return resolved;
        }

        internal static string ResolveLocale(string? localeTag) => MatchLocale(localeTag) ?? Fallback;

        // For tests.
        internal static bool HasRow(string key, string locale)
            => EnsureTable().TryGetValue(key, out var byTag) && byTag.ContainsKey(locale);

        // For tests: drops the loaded table so the next use reloads it, or with missingFile, stands in
        // for a strings file that failed to load.
        internal static void ResetForTests(bool missingFile = false)
        {
            s_Table = missingFile ? new Dictionary<string, Dictionary<string, string>>() : null;
            s_Locales = missingFile ? Array.Empty<string>() : null;
            s_Resolved.Clear();
        }

        static Dictionary<string, Dictionary<string, string>> EnsureTable()
        {
            if (s_Table != null)
            {
                return s_Table;
            }

            s_Table = Load() ?? new Dictionary<string, Dictionary<string, string>>();
            var locales = new List<string>();
            foreach (var byTag in s_Table.Values)
            {
                foreach (var tag in byTag.Keys)
                {
                    if (!locales.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                        locales.Add(tag);
                }
            }
            s_Locales = locales.AsReadOnly();
            return s_Table;
        }

        static Dictionary<string, Dictionary<string, string>>? Load()
        {
            var asset = Resources.Load<TextAsset>(k_ResourcePath);
            if (asset == null)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"MissingStringsFile: Resources/{k_ResourcePath}.json not found; the picker falls back to built-in English. " +
                    "Package install may be incomplete.");
                return null;
            }

            if (Json.Deserialize(asset.text) is not Dictionary<string, object> root)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"MalformedStringsFile: Resources/{k_ResourcePath}.json is not a JSON object; the picker falls back to built-in English.");
                return null;
            }

            var table = new Dictionary<string, Dictionary<string, string>>(root.Count);
            foreach (var entry in root)
            {
                // "_" keys are comments.
                if (entry.Key.StartsWith("_", StringComparison.Ordinal)
                    || entry.Value is not Dictionary<string, object> byTag)
                {
                    continue;
                }

                var translations = new Dictionary<string, string>(byTag.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var translation in byTag)
                {
                    if (translation.Value is string text)
                    {
                        translations[translation.Key] = text;
                    }
                }

                table[entry.Key] = translations;
            }

            return table;
        }
    }
}
