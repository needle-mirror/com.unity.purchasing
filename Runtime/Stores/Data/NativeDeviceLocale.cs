#nullable enable

using System;
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || ((UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR)
using System.Runtime.InteropServices;
#endif
#if (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.Purchasing.MiniJSON;
#endif

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Country, locale and currency as configured in the device's OS settings.
    /// Fields are null when the platform read failed or is unsupported.
    /// </summary>
    readonly struct DeviceLocale
    {
        /// <summary>ISO 3166-1 alpha-2, uppercase (e.g. "NG"), or null.</summary>
        public readonly string? CountryCode;

        /// <summary>BCP-47 tag (e.g. "en-NG"), or null.</summary>
        public readonly string? LocaleTag;

        /// <summary>ISO 4217 (e.g. "NGN"), or null.</summary>
        public readonly string? CurrencyCode;

        public DeviceLocale(string? countryCode, string? localeTag, string? currencyCode)
        {
            CountryCode = countryCode;
            LocaleTag = localeTag;
            CurrencyCode = currencyCode;
        }
    }

    /// <summary>
    /// Reads the device's region, locale and currency from native OS APIs.
    ///
    /// Unity Mono's <c>System.Globalization</c> only resolves cultures that have a
    /// Windows LCID, so combinations like English-in-Nigeria (en-NG) collapse to
    /// en-US and <c>RegionInfo.CurrentRegion</c> reports US/USD regardless of the
    /// OS region setting (UUM-151469). The native reads below bypass that.
    /// </summary>
    internal static class NativeDeviceLocale
    {
        // Reference-typed box so publication is one atomic volatile write (the struct
        // itself could tear across threads). A concurrent first read may run twice —
        // harmless, the read is idempotent.
        sealed class CachedRead
        {
            public readonly DeviceLocale Value;
            public CachedRead(DeviceLocale value) => Value = value;
        }

        static volatile CachedRead? s_Cache;

        /// <summary>Cached after the first successful read; never throws.</summary>
        public static DeviceLocale Read()
        {
            var cache = s_Cache;
            if (cache != null)
            {
                return cache.Value;
            }

            try
            {
                var read = ReadUncached();
                if (read == null)
                {
                    // The platform read failed without throwing (e.g. the native side
                    // wasn't ready to answer) — don't cache; the next call retries.
                    return default;
                }

                var (country, tag, currency) = read.Value;
                var value = Compose(country, tag, currency);
                s_Cache = new CachedRead(value);
                return value;
            }
            catch
            {
                // Possibly transient (e.g. a JNI call from an unattached thread) —
                // don't cache the failure; a later call on a good thread succeeds.
                return default;
            }
        }

        // The cache is process-static; tests exercising Read()'s caching reset it.
        internal static void ResetForTests()
        {
            s_Cache = null;
        }

        internal static bool HasCachedReadForTests => s_Cache != null;

        /// <summary>
        /// Combines a platform read into the published value: the country falls back to
        /// the locale's region subtag. The currency is passed through untouched — on
        /// Apple platforms and Android it comes from the same region setting as the
        /// country and is CLDR-current, which Mono's static region table is not.
        /// Windows applies <see cref="CurrencyCoherentWithCountry"/> before composing.
        /// </summary>
        internal static DeviceLocale Compose(string? countryCode, string? localeTag, string? currencyCode)
        {
            return new DeviceLocale(countryCode ?? CountryFromTag(localeTag), localeTag, currencyCode);
        }

        /// <summary>
        /// Windows-only coherence rule (kept platform-agnostic for tests): the currency
        /// is read from "Regional format" while the country comes from the independent
        /// "Country or region" setting. The currency is kept only when the format's region
        /// is that country: a player who set Country to Nigeria but left Regional format at
        /// English (US) would otherwise ship country=NG with currency=USD, and a regionless
        /// format (en-150, es-419) can't prove its currency (EUR, …) is Nigeria's either.
        /// A dropped currency is derived from the country by the caller instead.
        /// </summary>
        internal static string? CurrencyCoherentWithCountry(string? currencyCode, string? countryCode, string? localeTag)
        {
            var mismatch = currencyCode != null && countryCode != null && CountryFromTag(localeTag) != countryCode;
            return mismatch ? null : currencyCode;
        }

        /// <summary>
        /// Normalizes an OS locale identifier to a BCP-47 tag. Strips POSIX/ICU keywords
        /// ("en_NG@calendar=x" -> "en-NG") and BCP-47 extensions, which start at the first
        /// singleton subtag (Android 14+ Regional preferences: "en-US-u-fw-mon" -> "en-US",
        /// which Apple reports as @ keywords instead). An underscore separates the language tag from
        /// a trailing part: a region is appended only when the language tag doesn't carry
        /// one already ("zh-Hans_HK" -> "zh-Hans-HK", but Apple's combined
        /// "en-CA_NG" keeps the honest language preference "en-CA"); a non-region part is
        /// dropped (Windows alternate sorts: "de-DE_phoneb" -> "de-DE" — appending would
        /// build invalid BCP-47 that backend validation rejects). Null when nothing
        /// usable remains.
        /// </summary>
        internal static string? NormalizeTag(string? identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            var tag = identifier!;
            var at = tag.IndexOf('@');
            if (at >= 0)
            {
                tag = tag.Substring(0, at);
            }
            tag = tag.Trim('-', '_');
            if (tag.Length == 0)
            {
                return null;
            }

            var underscore = tag.LastIndexOf('_');
            if (underscore < 0)
            {
                return StripExtensions(tag);
            }

            var language = StripExtensions(tag.Substring(0, underscore).Replace('_', '-').Trim('-'));
            if (language.Length == 0)
            {
                return null;
            }

            var trailer = tag.Substring(underscore + 1);
            if (RegionSubtag(language) == null && IsRegion(trailer))
            {
                return language + "-" + trailer;
            }
            return language;
        }

        /// <summary>
        /// Extracts the region subtag from a locale tag ("en-NG" -> "NG",
        /// "zh-Hant-TW" -> "TW"). Null when the tag has no 2-letter region.
        /// </summary>
        internal static string? CountryFromTag(string? tag)
        {
            var normalized = NormalizeTag(tag);
            return normalized == null ? null : RegionSubtag(normalized);
        }

        // Cuts a tag at its first singleton subtag (u/t/x or any other single character):
        // everything after it is extensions, whose 2-letter keys ("ca" in
        // en-US-u-ca-gregory) are not regions. A singleton in first position is a whole
        // private-use/grandfathered tag ("x-private", "i-klingon") and is kept.
        static string StripExtensions(string tag)
        {
            var parts = tag.Split('-');
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 1)
                {
                    return string.Join("-", parts, 0, i);
                }
            }
            return tag;
        }

        // Region subtag of an already-normalized (extension-free) tag. In BCP-47 the
        // region is the 2nd or 3rd subtag (language, optional 4-letter script, region).
        static string? RegionSubtag(string normalizedTag)
        {
            var parts = normalizedTag.Split('-');
            for (var i = 1; i < parts.Length && i <= 2; i++)
            {
                var candidate = ValidAlpha2(parts[i]);
                if (candidate != null)
                {
                    return candidate;
                }
            }
            return null;
        }

        // BCP-47 region subtag shape: 2 letters (ISO 3166-1) or 3 digits (UN M.49).
        static bool IsRegion(string subtag)
        {
            if (ValidAlpha2(subtag) != null)
            {
                return true;
            }
            return subtag.Length == 3 && char.IsDigit(subtag[0]) && char.IsDigit(subtag[1]) && char.IsDigit(subtag[2]);
        }

        // Filters out UN M.49 numeric regions ("001", "419") and garbage.
        internal static string? ValidAlpha2(string? code)
        {
            return code != null && code.Length == 2 && char.IsLetter(code[0]) && char.IsLetter(code[1])
                ? code.ToUpperInvariant()
                : null;
        }

        internal static string? ValidCurrency(string? code)
        {
            return code != null && code.Length == 3
                             && char.IsLetter(code[0]) && char.IsLetter(code[1]) && char.IsLetter(code[2])
                ? code.ToUpperInvariant()
                : null;
        }

        // ReadUncached contract: null = the platform read failed, retry on the next
        // call. A non-null tuple is authoritative and cached, including the all-null
        // result on platforms with no native implementation.
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
        // Windows: country from the user's "Country or region" setting (Win10 1709+),
        // locale and currency from the user's regional format setting.

        // LOCALE_NAME_MAX_LENGTH = 85.
        const int k_LocaleNameMaxLength = 85;
        const uint k_LocaleSIntlSymbol = 0x00000015; // LOCALE_SINTLSYMBOL — ISO 4217 currency code

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetUserDefaultGeoName([Out] char[] geoName, int geoNameCount);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetUserDefaultLocaleName([Out] char[] localeName, int cchLocaleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetLocaleInfoEx(string? lpLocaleName, uint lcType, [Out] char[] lpLCData, int cchData);

        static (string? Country, string? Tag, string? Currency)? ReadUncached()
        {
            string? tag = null;
            try
            {
                var buffer = new char[k_LocaleNameMaxLength];
                var length = GetUserDefaultLocaleName(buffer, buffer.Length);
                if (length > 1)
                {
                    // Length includes the terminating null.
                    tag = NormalizeTag(new string(buffer, 0, length - 1));
                }
            }
            catch
            {
            }

            string? country = null;
            try
            {
                var buffer = new char[16];
                var length = GetUserDefaultGeoName(buffer, buffer.Length);
                if (length > 1)
                {
                    country = ValidAlpha2(new string(buffer, 0, length - 1));
                }
            }
            catch (EntryPointNotFoundException)
            {
                // Pre-1709 Windows 10 — fall back to the locale's region subtag.
            }

            string? currency = null;
            try
            {
                // Null locale name = LOCALE_NAME_USER_DEFAULT (the regional format setting).
                var buffer = new char[16];
                var length = GetLocaleInfoEx(null, k_LocaleSIntlSymbol, buffer, buffer.Length);
                if (length > 1)
                {
                    currency = ValidCurrency(new string(buffer, 0, length - 1));
                }
            }
            catch
            {
            }

            return (country, tag, CurrencyCoherentWithCountry(currency, country, tag));
        }

#elif UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
        // macOS: CoreFoundation directly — the prebuilt unitypurchasing.bundle is
        // not loaded in the Editor, so P/Invoking CF is the only path that also
        // covers Editor play mode, and it avoids a bundle rebuild.

        const string k_CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        const uint k_CFStringEncodingUTF8 = 0x08000100;
        // <dlfcn.h>: RTLD_DEFAULT ((void *) -2) — search every image already loaded.
        static readonly IntPtr k_RtldDefault = new IntPtr(-2);

        [DllImport(k_CoreFoundation)]
        static extern IntPtr CFLocaleCopyCurrent();

        [DllImport(k_CoreFoundation)]
        static extern IntPtr CFLocaleGetIdentifier(IntPtr locale);

        [DllImport(k_CoreFoundation)]
        static extern IntPtr CFLocaleGetValue(IntPtr locale, IntPtr key);

        [DllImport(k_CoreFoundation)]
        [return: MarshalAs(UnmanagedType.I1)]
        static extern bool CFStringGetCString(IntPtr str, byte[] buffer, long bufferSize, uint encoding);

        [DllImport(k_CoreFoundation)]
        static extern void CFRelease(IntPtr cf);

        [DllImport("/usr/lib/libSystem.dylib")]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        static (string? Country, string? Tag, string? Currency)? ReadUncached()
        {
            var locale = CFLocaleCopyCurrent();
            if (locale == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // CFLocaleGetIdentifier follows the Get rule — no release.
                var tag = NormalizeTag(CFStringToString(CFLocaleGetIdentifier(locale)));

                // Reading the country via kCFLocaleCountryCode (rather than parsing the
                // identifier) stays correct when the user sets a region override
                // (identifiers like "en_US@rg=ngzzzz").
                var country = ValidAlpha2(CFLocaleValueString(locale, "kCFLocaleCountryCode"));
                var currency = ValidCurrency(CFLocaleValueString(locale, "kCFLocaleCurrencyCode"));

                return (country, tag, currency);
            }
            finally
            {
                // CFLocaleCopyCurrent follows the Copy rule.
                CFRelease(locale);
            }
        }

        static string? CFLocaleValueString(IntPtr locale, string constantName)
        {
            var key = CFConstant(constantName);
            // A NULL key crashes inside CoreFoundation — the managed catch can't stop that.
            return key == IntPtr.Zero ? null : CFStringToString(CFLocaleGetValue(locale, key));
        }

        // kCFLocale* keys are exported CFStringRef constants; resolve via dlsym and
        // dereference. No dlopen handle is needed: ReadUncached has already P/Invoked
        // CFLocaleCopyCurrent, so CoreFoundation is loaded by the time this runs.
        static IntPtr CFConstant(string symbolName)
        {
            var symbol = dlsym(k_RtldDefault, symbolName);
            return symbol == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(symbol);
        }

        static string? CFStringToString(IntPtr cfString)
        {
            if (cfString == IntPtr.Zero)
            {
                return null;
            }

            var buffer = new byte[128];
            if (!CFStringGetCString(cfString, buffer, buffer.Length, k_CFStringEncodingUTF8))
            {
                return null;
            }

            var length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
            {
                length = buffer.Length;
            }
            return length == 0 ? null : System.Text.Encoding.UTF8.GetString(buffer, 0, length);
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        static (string? Country, string? Tag, string? Currency)? ReadUncached()
        {
            // Reads the locale from Resources.getSystem() rather than Locale.getDefault(),
            // which may not match the OS setting. Country, tag and currency all derive
            // from this one locale.
            // Caveat (API 33+): if the game sets a per-app language (localeConfig /
            // LocaleManager.setApplicationLocales), that override can reach this
            // configuration too, so it may report the app's language instead of the
            // system one. LocaleManager.getSystemLocales() is the reliable source there.
            using var resources = new AndroidJavaClass("android.content.res.Resources");
            using var systemResources = resources.CallStatic<AndroidJavaObject>("getSystem");
            using var configuration = systemResources.Call<AndroidJavaObject>("getConfiguration");
            using var version = new AndroidJavaClass("android.os.Build$VERSION");

            AndroidJavaObject? locale = null;
            try
            {
                if (version.GetStatic<int>("SDK_INT") >= 24)
                {
                    using var locales = configuration.Call<AndroidJavaObject>("getLocales");
                    locale = locales.Call<AndroidJavaObject>("get", 0);
                }
                else
                {
                    locale = configuration.Get<AndroidJavaObject>("locale");
                }

                if (locale == null)
                {
                    return null;
                }

                var tag = NormalizeTag(locale.Call<string>("toLanguageTag"));
                // getCountry() can be empty or a UN M.49 numeric — ValidAlpha2 filters those.
                var country = ValidAlpha2(locale.Call<string>("getCountry"));

                string? currency = null;
                try
                {
                    using var currencyClass = new AndroidJavaClass("java.util.Currency");
                    using var currencyInstance = currencyClass.CallStatic<AndroidJavaObject>("getInstance", locale);
                    currency = ValidCurrency(currencyInstance?.Call<string>("getCurrencyCode"));
                }
                catch
                {
                    // Currency.getInstance throws for locales without a valid country.
                }

                return (country, tag, currency);
            }
            finally
            {
                locale?.Dispose();
            }
        }

#elif (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "unityPurchasing_GetDeviceLocale")]
        static extern IntPtr GetDeviceLocaleNative();

        [DllImport("__Internal", EntryPoint = "unityPurchasing_DeallocateMemory")]
        static extern void DeallocateMemory(IntPtr pointer);

        static (string? Country, string? Tag, string? Currency)? ReadUncached()
        {
            var pointer = GetDeviceLocaleNative();
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            string? json;
            try
            {
                json = Marshal.PtrToStringAuto(pointer);
            }
            finally
            {
                DeallocateMemory(pointer);
            }

            if (string.IsNullOrEmpty(json) || Json.Deserialize(json) is not Dictionary<string, object> data)
            {
                return null;
            }

            var tag = NormalizeTag(data.TryGetString("localeIdentifier"));
            var country = ValidAlpha2(data.TryGetString("countryCode"));
            var currency = ValidCurrency(data.TryGetString("currencyCode"));
            return (country, tag, currency);
        }

#else
        static (string? Country, string? Tag, string? Currency)? ReadUncached()
        {
            // WebGL, Linux, consoles: no native read yet — callers fall back to
            // System.Globalization as before. Authoritative (cached): a retry can
            // never produce anything different here.
            return (null, null, null);
        }
#endif
    }
}
