#nullable enable
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
        //IFirebaseAnalyticsClient? m_FirebaseAnalyticsClient;
        INativeAppleStore? m_NativeStore;
        // AppInstanceId is stable per session and re-fetching does JNI thread-attach work.
        string? m_CachedAppInstanceId;
        string? m_CachedFirebaseSessionId;
        string? m_CachedFirebaseAppId;
        // Minted by the client that begins a purchase journey (typically the
        // Purchase Options UI). No SDK-side source today; populated by a future
        // setter / call-site once that UI lands.
        string? m_CachedImpressionId;

        [Preserve]
        internal PlayerData(ICoreRegistryHelper coreRegistry, IUtil util, IStoreLocationContext storeLocationContext)
        {
            m_CoreRegistry = coreRegistry;
            m_Util = util;
            m_StoreLocationContext = storeLocationContext;
        }

#if ENABLE_UNITY_CONSENT
        ConsentState ConsentState => EndUserConsent.GetConsentState();
        static string[] s_ConsentStatesStrings = {"unspecified", "granted", "denied"};
        string ConsentStateAdsIntent => s_ConsentStatesStrings[(int) ConsentState.AdsIntent];
        string ConsentStateAnalyticsIntent => s_ConsentStatesStrings[(int) ConsentState.AnalyticsIntent];
#endif

#pragma warning disable CS1998
        public async Task<PlayerIdentity> CreatePlayerIdentityAsync()
#pragma warning restore CS1998
        {
            string? idfa = null;
            string? idfv = null;
            string? gaid = null;
            string? appInstanceId = null;
            string? firebaseSessionId = null;
            string? firebaseAppId = null;
            string? impressionId = m_CachedImpressionId;
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
                        m_AdvertisingIdClient ??= new GoogleAdvertisingIdClient();
                        gaid = m_AdvertisingIdClient.FetchGaid();
                        //m_FirebaseAnalyticsClient ??= new FirebaseAnalyticsClient();
                        //appInstanceId = m_CachedAppInstanceId ??= await m_FirebaseAnalyticsClient.FetchAppInstanceIdAsync();
                        //firebaseSessionId = m_CachedFirebaseSessionId ??= await m_FirebaseAnalyticsClient.FetchSessionIdAsync();
                        //firebaseAppId = m_CachedFirebaseAppId ??= await m_FirebaseAnalyticsClient.FetchAppIdAsync();
                    }
                    break;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
            case RuntimePlatform.VisionOS:
#endif
                    m_NativeStore ??= new NativeStoreProvider().GetStorekit();
                    if (adsIntentGranted)
                    {
                        idfa = m_NativeStore.FetchAdvertisingIdentifier();
                    }

                    idfv = m_NativeStore.FetchVendorIdentifier();
                    break;
            }


            return new PlayerIdentity(
                unityInstallationId: m_CoreRegistry.InstallationId,
                sessionId: m_SessionId,
                unityAnalyticsId: m_AnalyticsId,
                unityIdfa: idfa,
                unityIdfv: idfv,
                unityGaid: gaid,
                unityAppInstanceId: appInstanceId,
                unityFirebaseSessionId: firebaseSessionId,
                unityFirebaseAppId: firebaseAppId,
                unityImpressionId: impressionId,
                unityIapSdkVersion: IAPVersion.Current,
                unityEngineVersion: m_Util.unityVersion,
                unityUserId: m_CoreRegistry.ExternalUserId,
                unityInstallationTimestamp: AppInstallInfo.GetInstallTimestamp() ?? default
#if ENABLE_UNITY_CONSENT
                , unityConsentStateAdsIntent: ConsentStateAdsIntent
                , unityConsentStateAnalyticsIntent: ConsentStateAnalyticsIntent
#endif
            );
        }

        public string? Locale => GetCurrentLocaleCodeIfValid();

        // Store-provided values (set asynchronously via StoreLocationContext) take precedence
        // over the device-locale defaults; falls back to the device region when no store value is known.
        public string? RegionCode => m_StoreLocationContext.CountryCode ?? GetDefaultRegionInfo()?.TwoLetterISORegionName;
        public string? CurrencyCode => m_StoreLocationContext.CurrencyCode ?? GetDefaultRegionInfo()?.ISOCurrencySymbol;

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
