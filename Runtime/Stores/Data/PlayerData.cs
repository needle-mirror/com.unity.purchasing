#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Uniject;
#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
using Unity.Services.Analytics;
using Unity.Services.Core;
#endif
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.PaymentProviderService.Models;
using UnityEngine.Purchasing.Registration;
using UnityEngine.Scripting;
#if ENABLE_UNITY_CONSENT
using UnityEngine.UnityConsent;
#endif

namespace UnityEngine.Purchasing.Stores
{
    internal class PlayerData : IPlayerData
    {
        public string DisplayName { get; set; } = "";

        readonly ICoreRegistryHelper m_CoreRegistry;
        readonly IUtil m_Util;
        readonly IStoreLocationContext m_StoreLocationContext;
        readonly Func<DeviceLocale> m_DeviceLocaleReader;
        string? m_SessionId => PlayerPrefs.GetString("unity_connect.session_id", null);
#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
        // Accessed during PlayerIdentity construction on the PSP purchase path.
        // AnalyticsService.Instance throws ServicesInitializationException when
        // Unity Services has been initialized but the Analytics component hasn't
        // (common when the project has com.unity.services.analytics installed but
        // has never called StartDataCollection). PlayerIdentity treats
        // unityAnalyticsId as optional, so swallow and return null — otherwise the
        // exception escapes through OpenURL and fails the purchase with a generic
        // "Unknown error: The Analytics service has not been initialized."
        string? m_AnalyticsId
        {
            get
            {
                try
                {
                    return AnalyticsService.Instance.GetAnalyticsUserID();
                }
                catch (ServicesInitializationException)
                {
                    return null;
                }
            }
        }
#else
        string? m_AnalyticsId => null;
#endif

        IGoogleAdvertisingIdClient? m_AdvertisingIdClient;
        IFirebaseAnalyticsClient? m_FirebaseAnalyticsClient;
        ConnectionsSettingsClient? m_ConnectionsSettingsClient;
        INativeAppleStore? m_NativeStore;
        // AppInstanceId is stable per session and re-fetching does JNI thread-attach work.
        string? m_CachedAppInstanceId;
        string? m_CachedFirebaseSessionId;
        bool m_FirebaseSessionIdTimedOut;
        string? m_CachedFirebaseAppId;

        [Preserve]
        [Inject]
        internal PlayerData(ICoreRegistryHelper coreRegistry, IUtil util, IStoreLocationContext storeLocationContext)
            : this(coreRegistry, util, storeLocationContext, NativeDeviceLocale.Read)
        {
        }

        // Test seam — production always reads the device's OS settings.
        internal PlayerData(ICoreRegistryHelper coreRegistry, IUtil util, IStoreLocationContext storeLocationContext,
            Func<DeviceLocale> deviceLocaleReader)
        {
            m_CoreRegistry = coreRegistry;
            m_Util = util;
            m_StoreLocationContext = storeLocationContext;
            m_DeviceLocaleReader = deviceLocaleReader;

            // Warm the native locale cache from this thread (DI construction runs on the
            // main thread). On Android the JNI read fails on unattached threads, so a
            // game that only touches the properties from Task continuations would
            // otherwise never get a successful read.
            NativeDeviceLocale.Read();
        }

#if ENABLE_UNITY_CONSENT
        ConsentState ConsentState => EndUserConsent.GetConsentState();
        static string[] s_ConsentStatesStrings = {"unspecified", "granted", "denied"};
        string ConsentStateAdsIntent => s_ConsentStatesStrings[(int) ConsentState.AdsIntent];
        string ConsentStateAnalyticsIntent => s_ConsentStatesStrings[(int) ConsentState.AnalyticsIntent];
#endif

        public async Task<PlayerIdentity> CreatePlayerIdentityAsync(string? impressionId = null)
        {
            string? idfa = null;
            string? idfv = null;
            string? gaid = null;
            string? appInstanceId = null;
            string? firebaseSessionId = null;
            string? firebaseAppId = null;
            bool adsIntentGranted = false;

#if ENABLE_UNITY_CONSENT
            if (ConsentState.AdsIntent == ConsentStatus.Granted)
            {
                adsIntentGranted = true;
            }
#endif

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    if (adsIntentGranted)
                    {
                        var enabledAttributes = EnabledAttributes;
                        if (enabledAttributes.Contains(ConnectionsSettingsClient.GaidKey))
                        {
                            m_AdvertisingIdClient ??= new GoogleAdvertisingIdClient();
                            gaid = await m_AdvertisingIdClient.FetchGaidAsync();
                        }
                        m_FirebaseAnalyticsClient ??= new FirebaseAnalyticsClient();
                        (appInstanceId, firebaseSessionId, firebaseAppId) = await FetchEnabledFirebaseIdsAsync(m_FirebaseAnalyticsClient);
                    }
                    break;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
            case RuntimePlatform.VisionOS:
#endif
                    m_NativeStore ??= new NativeStoreProvider().GetStorekit();
                    if (adsIntentGranted && EnabledAttributes.Contains(ConnectionsSettingsClient.IdfaKey))
                    {
                        idfa = m_NativeStore.FetchAdvertisingIdentifier();
                    }

                    idfv = m_NativeStore.FetchVendorIdentifier();
                    if (adsIntentGranted)
                    {
                        m_FirebaseAnalyticsClient ??= new AppleFirebaseAnalyticsClient();
                        (appInstanceId, firebaseSessionId, firebaseAppId) = await FetchEnabledFirebaseIdsAsync(m_FirebaseAnalyticsClient);
                    }
                    break;
            }


            return new PlayerIdentity(
                unityImpressionId: impressionId,
                unityInstallationId: m_CoreRegistry.InstallationId,
                sessionId: m_SessionId,
                unityAnalyticsId: m_AnalyticsId,
                unityIdfa: idfa,
                unityIdfv: idfv,
                unityGaid: gaid,
                unityAppInstanceId: appInstanceId,
                unityFirebaseSessionId: firebaseSessionId,
                unityFirebaseAppId: firebaseAppId,
                unityIapSdkVersion: IAPVersion.Current,
                unityEngineVersion: m_Util.unityVersion,
                unityApplicationVersion: m_Util.gameVersion,
                unityUserId: m_CoreRegistry.ExternalUserId,
                unityInstallationTimestamp: AppInstallInfo.GetInstallTimestamp() ?? default
#if ENABLE_UNITY_CONSENT
                , unityConsentStateAdsIntent: ConsentStateAdsIntent
                , unityConsentStateAnalyticsIntent: ConsentStateAnalyticsIntent
#endif
            );
        }

        // Shared by the Android and Apple branches: fetches each Firebase
        // identifier the remote connections settings allow, caching non-null results.
        internal async Task<(string? appInstanceId, string? firebaseSessionId, string? firebaseAppId)> FetchEnabledFirebaseIdsAsync(IFirebaseAnalyticsClient client)
        {
            var enabledAttributes = EnabledAttributes;
            string? appInstanceId = null;
            string? firebaseSessionId = null;
            string? firebaseAppId = null;
            // Google Analytics seems to bind its ids at configure: before it, the app id is the plist fallback, the app
            // instance id can still be reset, and the session id is always null. Ids are cached only once configured,
            // as Google Analytics seems to keep the first configured app's ids until relaunch (observed with Firebase
            // iOS SDK 10.29 and 12.19). Android has no configure step.
            var defaultAppConfigured = !(client is IFirebaseDefaultAppState state) || state.IsDefaultAppConfigured;
            if (enabledAttributes.Contains(ConnectionsSettingsClient.AppInstanceIdKey))
            {
                appInstanceId = m_CachedAppInstanceId ?? await client.FetchAppInstanceIdAsync();
                if (defaultAppConfigured)
                {
                    m_CachedAppInstanceId ??= appInstanceId;
                }
            }
            // Not requested before configure, where it is always null.
            if (enabledAttributes.Contains(ConnectionsSettingsClient.FirebaseSessionIdKey) && defaultAppConfigured && !m_FirebaseSessionIdTimedOut)
            {
                if (m_CachedFirebaseSessionId == null)
                {
                    var (sessionId, timedOut) = await client.FetchSessionIdAsync();
                    m_CachedFirebaseSessionId = sessionId;
                    // A null session id that hit the native timeout is cached, so later identities skip the 2 s wait.
                    m_FirebaseSessionIdTimedOut = (sessionId == null) && timedOut;
                }
                firebaseSessionId = m_CachedFirebaseSessionId;
            }
            if (enabledAttributes.Contains(ConnectionsSettingsClient.FirebaseAppIdKey))
            {
                firebaseAppId = m_CachedFirebaseAppId ?? await client.FetchAppIdAsync();
                if (defaultAppConfigured)
                {
                    m_CachedFirebaseAppId ??= firebaseAppId;
                }
            }
            return (appInstanceId, firebaseSessionId, firebaseAppId);
        }

        // Which Google Analytics identifiers the project allows collecting,
        // per remote connections settings. Non-blocking: empty (collect
        // nothing) until the session's background fetch has succeeded, so
        // identity creation never waits on the network.
        HashSet<string> EnabledAttributes
        {
            get
            {
                m_ConnectionsSettingsClient ??= new ConnectionsSettingsClient(m_CoreRegistry);
                return m_ConnectionsSettingsClient.CachedEnabledAttributes;
            }
        }

        // False while the Apple client reports the default app unconfigured; always false on macOS and
        // visionOS, where the client is a stub. The Apple client is created here if needed, so this holds
        // before the first identity is created too.
        public bool FirebaseDefaultAppConfigured
        {
            get
            {
                if (IsApplePlatform)
                {
                    m_FirebaseAnalyticsClient ??= new AppleFirebaseAnalyticsClient();
                }
                return !(m_FirebaseAnalyticsClient is IFirebaseDefaultAppState state) || state.IsDefaultAppConfigured;
            }
        }

        static bool IsApplePlatform =>
            Application.platform == RuntimePlatform.IPhonePlayer
            || Application.platform == RuntimePlatform.OSXPlayer
            || Application.platform == RuntimePlatform.tvOS
#if UNITY_VISIONOS
            || Application.platform == RuntimePlatform.VisionOS
#endif
            ;

        // Fallback chain: store-provided values (set asynchronously via StoreLocationContext)
        // take precedence, then the device's OS settings read natively, then .NET globalization.
        // The .NET tier is last because Unity Mono collapses locales without a Windows LCID
        // (e.g. en-NG) to en-US, reporting US/USD regardless of the OS region (UUM-151469).
        // Every source is read exactly once, so the three values can't mix a failed read with
        // a successful one, nor straddle an asynchronous store write.
        public CheckoutLocation GetCheckoutLocation()
        {
            var (storeCountry, storeCurrency) = m_StoreLocationContext.Snapshot();
            var deviceLocale = m_DeviceLocaleReader();
            var defaultRegion = GetDefaultRegionInfo();

            var locale = deviceLocale.LocaleTag ?? GetCurrentLocaleCodeIfValid();
            var country = storeCountry ?? deviceLocale.CountryCode ?? defaultRegion?.TwoLetterISORegionName;
            return new CheckoutLocation(locale, country, SelectCurrency(storeCountry, storeCurrency, deviceLocale, defaultRegion));
        }

        // The currency must stay coherent with the country the order is created against: it
        // is only ever taken from the source that supplied that country, or derived from
        // the country itself — never borrowed from another source. Store country and
        // currency are written at different times (the storefront at connect, the currency
        // from fetched products, or never), so either can be present without the other.
        // A null currency is safe: the backend and SelectCurrency both fall back to the
        // first catalog currency.
        static string? SelectCurrency(string? storeCountry, string? storeCurrency, DeviceLocale deviceLocale, RegionInfo? defaultRegion)
        {
            if (storeCountry != null)
            {
                if (storeCurrency != null)
                {
                    return storeCurrency;
                }

                // The device agrees on the country: its OS currency is CLDR-current, where
                // Mono's region table is frozen.
                if (storeCountry == deviceLocale.CountryCode && deviceLocale.CurrencyCode != null)
                {
                    return deviceLocale.CurrencyCode;
                }

                // Null when the region table can't resolve the storefront (e.g. XK).
                return CurrencyForCountry(storeCountry);
            }

            // A store currency without a store country (SK1, a failed Google billing-config
            // read) has no country of its own here, so it is not used.
            if (deviceLocale.CountryCode != null)
            {
                return deviceLocale.CurrencyCode ?? CurrencyForCountry(deviceLocale.CountryCode);
            }

            // No country from any source: the country is .NET's, and so is the currency.
            return CurrencyOf(defaultRegion);
        }

        // Hand-kept corrections to Mono's frozen region table, checked against the Unity
        // dashboard's catalog currencies: currency changes the table predates (Croatia 2023,
        // Bulgaria 2026 — both to EUR) and countries it lacks entirely (Cyprus, Tanzania, and
        // Kosovo, on the euro since 2002). Add a country when it switches currency or a
        // dashboard currency's country is missing.
        static readonly Dictionary<string, string> s_RegionTableCurrencyCorrections = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HR", "EUR" },
            { "BG", "EUR" },
            { "CY", "EUR" },
            { "TZ", "TZS" },
            { "XK", "EUR" },
        };

        // Mono's region table is intact (new RegionInfo("NG") -> NGN works); it's only
        // culture->region resolution that collapses, so this is a reliable currency
        // source for a natively-read country when the OS currency read failed.
        internal static string? CurrencyForCountry(string? alpha2CountryCode)
        {
            if (string.IsNullOrEmpty(alpha2CountryCode))
            {
                return null;
            }

            // Before RegionInfo: it throws for the countries the table lacks.
            if (s_RegionTableCurrencyCorrections.TryGetValue(alpha2CountryCode!, out var corrected))
            {
                return corrected;
            }

            try
            {
                return new RegionInfo(alpha2CountryCode).ISOCurrencySymbol;
            }
            catch
            {
                // Country code unknown to (or unresolvable by) the runtime's region table.
                return null;
            }
        }

        // The .NET default region's currency, with the same corrections applied.
        static string? CurrencyOf(RegionInfo? region)
        {
            if (region == null)
            {
                return null;
            }
            return s_RegionTableCurrencyCorrections.TryGetValue(region.TwoLetterISORegionName, out var corrected)
                ? corrected
                : region.ISOCurrencySymbol;
        }

        private string? GetCurrentLocaleCodeIfValid()
        {
            string name;
            try
            {
                name = CultureInfo.CurrentCulture.Name;
            }
            catch
            {
                // CultureInfo.CurrentCulture can throw an ArgumentNullException
                return null;
            }

            return name;
        }

        private RegionInfo? GetDefaultRegionInfo()
        {
            try
            {
                return RegionInfo.CurrentRegion;
            }
            catch
            {
                return null;
            }
        }
    }
}
