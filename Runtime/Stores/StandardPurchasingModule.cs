using System;
using System.Collections.Generic;
using Uniject;

using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.Utils;

#if UNITY_PURCHASING_GPBL
using UnityEngine.Purchasing.GooglePlayBilling;
#endif

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Module for the standard stores covered by Unity;
    /// Apple App store, Google Play and more.
    /// </summary>
    public class StandardPurchasingModule : AbstractPurchasingModule, IAndroidStoreSelection
    {
        /// <summary>
        /// Obsolete and inaccurate. Do not use.
        /// </summary>
        [Obsolete("Not accurate. Use Version instead.", false)]
        public const string k_PackageVersion = "3.0.1";
        internal readonly string k_Version = "4.5.1"; // NOTE: Changed using GenerateUnifiedIAP.sh before pack step.
        /// <summary>
        /// The version of com.unity.purchasing installed and the app was built using.
        /// </summary>
        public string Version => k_Version;

        private readonly INativeStoreProvider m_NativeStoreProvider;
        private readonly RuntimePlatform m_RuntimePlatform;
        private static StandardPurchasingModule ModuleInstance;

        internal IUtil util { get; private set; }
        internal ILogger logger { get; private set; }
        internal StoreInstance storeInstance { get; private set; }
        internal ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper { get; set; }
        internal ITelemetryDiagnosticsInstanceWrapper telemetryDiagnosticsInstanceWrapper { get; set; }
        // Map Android store enums to their public names.
        // Necessary because store enum names and public names almost, but not quite, match.
        private static readonly Dictionary<AppStore, string> AndroidStoreNameMap = new Dictionary<AppStore, string>() {
            { AppStore.AmazonAppStore, AmazonApps.Name },
            { AppStore.GooglePlay, GooglePlay.Name },
            { AppStore.UDP, UDP.Name},
            { AppStore.NotSpecified, GooglePlay.Name }
        };

        internal class StoreInstance
        {
            internal string storeName { get; }
            internal IStore instance { get; }
            internal StoreInstance(string name, IStore instance)
            {
                storeName = name;
                this.instance = instance;
            }
        }

        internal StandardPurchasingModule(IUtil util, ILogger logger, INativeStoreProvider nativeStoreProvider,
            RuntimePlatform platform, AppStore android, ITelemetryDiagnosticsInstanceWrapper telemetryDiagnosticsInstanceWrapper, ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper)
        {
            this.util = util;
            this.logger = logger;
            m_NativeStoreProvider = nativeStoreProvider;
            m_RuntimePlatform = platform;
            useFakeStoreUIMode = FakeStoreUIMode.Default;
            useFakeStoreAlways = false;
            appStore = android;
            this.telemetryDiagnosticsInstanceWrapper = telemetryDiagnosticsInstanceWrapper;
            this.telemetryMetricsInstanceWrapper = telemetryMetricsInstanceWrapper;
        }

        /// <summary>
        /// A property that retrieves the <c>AppStore</c> type.
        /// </summary>
        public AppStore appStore { get; private set; }

        // At some point we should remove this but to do so will cause a compile error
        // for App developers who used this property directly.
        private readonly bool usingMockMicrosoft;

        /// <summary>
        /// The UI mode for the Fake store, if it's in use.
        /// </summary>
        public FakeStoreUIMode useFakeStoreUIMode { get; set; }

        /// <summary>
        /// Whether or not to use the Fake store.
        /// </summary>
        public bool useFakeStoreAlways { get; set; }

        /// <summary>
        /// Creates an instance of StandardPurchasingModule or retrieves the existing one.
        /// </summary>
        /// <returns> The existing instance or the one just created. </returns>
        public static StandardPurchasingModule Instance()
        {
            return Instance(AppStore.NotSpecified);
        }

        /// <summary>
        /// Creates an instance of StandardPurchasingModule or retrieves the existing one, specifying a type of App store.
        /// </summary>
        /// <param name="androidStore"> The type of Android Store with which to create the instance. </param>
        /// <returns> The existing instance or the one just created. </returns>
        public static StandardPurchasingModule Instance(AppStore androidStore)
        {
            if (null == ModuleInstance)
            {
                var logger = Debug.unityLogger;
                var gameObject = new GameObject("IAPUtil");
                Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                var util = gameObject.AddComponent<UnityUtil>();

                var textAsset = Resources.Load("BillingMode") as TextAsset;
                StoreConfiguration config = null;
                if (null != textAsset)
                {
                    config = StoreConfiguration.Deserialize(textAsset.text);
                }

                // No Android target specified at runtime, use the build time setting.
                if (androidStore == AppStore.NotSpecified)
                {
                    // Default to Google Play if we don't have a build time store selection.
                    androidStore = AppStore.GooglePlay;

                    if (null != config)
                    {
                        var buildTimeStore = config.androidStore;
                        if (buildTimeStore != AppStore.NotSpecified)
                        {
                            androidStore = buildTimeStore;
                        }
                    }
                }

                ModuleInstance = new StandardPurchasingModule(
                    util,
                    logger,
                    new NativeStoreProvider(),
                    Application.platform,
                    androidStore,
                    new TelemetryDiagnosticsInstanceWrapper(),
                    new TelemetryMetricsInstanceWrapper());
            }

            return ModuleInstance;
        }

        /// <summary>
        /// Configures the StandardPurchasingModule.
        /// </summary>
        public override void Configure()
        {
            BindConfiguration<IGooglePlayConfiguration>(new FakeGooglePlayStoreConfiguration());
            BindExtension<IGooglePlayStoreExtensions>(new FakeGooglePlayStoreExtensions());

            BindConfiguration<IAppleConfiguration>(new FakeAppleConfiguration());
            BindExtension<IAppleExtensions>(new FakeAppleExtensions());

            BindConfiguration<IAmazonConfiguration>(new FakeAmazonExtensions());
            BindExtension<IAmazonExtensions>(new FakeAmazonExtensions());

            BindConfiguration<IMicrosoftConfiguration>(new MicrosoftConfiguration(this));
            BindExtension<IMicrosoftExtensions>(new FakeMicrosoftExtensions());

            BindConfiguration<IAndroidStoreSelection>(this);

            BindExtension<IUDPExtensions>(new FakeUDPExtension());
            BindExtension<ITransactionHistoryExtensions>(new FakeTransactionHistoryExtensions());

            // Our store implementations are singletons, we must not attempt to instantiate
            // them more than once.
            if (null == storeInstance)
            {
                storeInstance = InstantiateStore();
            }

            RegisterStore(storeInstance.storeName, storeInstance.instance);

            // Moving SetModule from reflection to an interface
            var internalStore = storeInstance.instance as IStoreInternal;
            if (internalStore != null)
            {
                // NB: as currently implemented this is also doing Init work for ManagedStore
                internalStore.SetModule(this);
            }

            // If we are using a JSONStore, bind to it to get transaction history.
            if ((util != null) && util.IsClassOrSubclass(typeof(JSONStore), storeInstance.instance.GetType()))
            {
                var jsonStore = (JSONStore)storeInstance.instance;
                BindExtension<ITransactionHistoryExtensions>(jsonStore);
            }
        }

        private StoreInstance InstantiateStore()
        {
            if (useFakeStoreAlways)
            {
                return new StoreInstance(FakeStore.Name, InstantiateFakeStore());
            }

            switch (m_RuntimePlatform)
            {
                case RuntimePlatform.OSXPlayer:
                    appStore = AppStore.MacAppStore;
                    return new StoreInstance(MacAppStore.Name, InstantiateApple());
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
                    appStore = AppStore.AppleAppStore;
                    return new StoreInstance(AppleAppStore.Name, InstantiateApple());
                case RuntimePlatform.Android:
                    switch (appStore)
                    {
                        case AppStore.UDP:
                            return new StoreInstance(AndroidStoreNameMap[appStore], InstantiateUDP());
                        default:
                            return new StoreInstance(AndroidStoreNameMap[appStore], InstantiateAndroid());
                    }
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    appStore = AppStore.WinRT;
                    return new StoreInstance(WindowsStore.Name, instantiateWindowsStore());
            }
            appStore = AppStore.fake;
            return new StoreInstance(FakeStore.Name, InstantiateFakeStore());
        }

        private IStore InstantiateAndroid()
        {
            if (appStore == AppStore.GooglePlay)
            {
                return InstantiateGoogleStore();
            }
            else
            {
                var telemetryMetrics = new TelemetryMetricsService(telemetryMetricsInstanceWrapper);
                var store = new MetricizedJsonStore(telemetryMetrics);
                return InstantiateAndroidHelper(store);
            }
        }

        private IStore InstantiateGoogleStore()
        {
            IGooglePurchaseCallback googlePurchaseCallback = new GooglePlayPurchaseCallback(util);
            IGoogleProductCallback googleProductCallback = new GooglePlayProductCallback();

            var googlePlayStoreService = BuildAndInitGooglePlayStoreServiceAar(googlePurchaseCallback, googleProductCallback);

            IGooglePlayStorePurchaseService googlePlayStorePurchaseService = new GooglePlayStorePurchaseService(googlePlayStoreService);
            IGooglePlayStoreFinishTransactionService googlePlayStoreFinishTransactionService = new GooglePlayStoreFinishTransactionService(googlePlayStoreService);
            IGoogleFetchPurchases googleFetchPurchases = new GoogleFetchPurchases(googlePlayStoreService, util);
            var googlePlayConfiguration = BuildGooglePlayStoreConfiguration(googlePlayStoreService, googlePurchaseCallback, googleProductCallback);
            var telemetryDiagnostics = new TelemetryDiagnostics(telemetryDiagnosticsInstanceWrapper);
            var telemetryMetrics = new TelemetryMetricsService(telemetryMetricsInstanceWrapper);
            IGooglePlayStoreRetrieveProductsService googlePlayStoreRetrieveProductsService = new GooglePlayStoreRetrieveProductsService(
                googlePlayStoreService,
                googleFetchPurchases,
                googlePlayConfiguration);
            var googlePlayStoreExtensions = new MetricizedGooglePlayStoreExtensions(
                googlePlayStoreService,
                googlePlayStoreFinishTransactionService,
                logger,
                telemetryDiagnostics,
                telemetryMetrics);

            var googlePlayStore = new GooglePlayStore(
                googlePlayStoreRetrieveProductsService,
                googlePlayStorePurchaseService,
                googleFetchPurchases,
                googlePlayStoreFinishTransactionService,
                googlePurchaseCallback,
                googlePlayConfiguration,
                googlePlayStoreExtensions,
                util);
            util.AddPauseListener(googlePlayStore.OnPause);
            BindGoogleConfiguration(googlePlayConfiguration);
            BindGoogleExtension(googlePlayStoreExtensions);
            return googlePlayStore;
        }

        void BindGoogleExtension(GooglePlayStoreExtensions googlePlayStoreExtensions)
        {
            BindExtension<IGooglePlayStoreExtensions>(googlePlayStoreExtensions);
        }

        static GooglePlayConfiguration BuildGooglePlayStoreConfiguration(IGooglePlayStoreService googlePlayStoreService,
            IGooglePurchaseCallback googlePurchaseCallback, IGoogleProductCallback googleProductCallback)
        {
            var googlePlayConfiguration = new GooglePlayConfiguration(googlePlayStoreService);
            googlePurchaseCallback.SetStoreConfiguration(googlePlayConfiguration);
            googleProductCallback.SetStoreConfiguration(googlePlayConfiguration);
            return googlePlayConfiguration;
        }

        void BindGoogleConfiguration(GooglePlayConfiguration googlePlayConfiguration)
        {
            BindConfiguration<IGooglePlayConfiguration>(googlePlayConfiguration);
        }

        IGooglePlayStoreService BuildAndInitGooglePlayStoreServiceAar(IGooglePurchaseCallback googlePurchaseCallback,
            IGoogleProductCallback googleProductCallback)
        {
            var googleCachedQuerySkuDetailsService = new GoogleCachedQuerySkuDetailsService();
            var googleLastKnownProductService = new GoogleLastKnownProductService();
            var googlePurchaseStateEnumProvider = new GooglePurchaseStateEnumProvider();
            var googlePurchaseBuilder = new GooglePurchaseBuilder(googleCachedQuerySkuDetailsService, logger);
            var googlePurchaseUpdatedListener = new GooglePurchaseUpdatedListener(googleLastKnownProductService,
                googlePurchaseCallback, googlePurchaseBuilder, googleCachedQuerySkuDetailsService,
                googlePurchaseStateEnumProvider);
            var googleBillingClient = new GoogleBillingClient(googlePurchaseUpdatedListener, util);
            var skuDetailsConverter = new SkuDetailsConverter();
            var retryPolicy = new ExponentialRetryPolicy();
            var googleQuerySkuDetailsService = new QuerySkuDetailsService(googleBillingClient, googleCachedQuerySkuDetailsService, skuDetailsConverter, retryPolicy, googleProductCallback);
            var purchaseService = new GooglePurchaseService(googleBillingClient, googlePurchaseCallback, googleQuerySkuDetailsService);
            var queryPurchasesService = new GoogleQueryPurchasesService(googleBillingClient, googlePurchaseBuilder);
            var finishTransactionService = new GoogleFinishTransactionService(googleBillingClient, queryPurchasesService);
            var billingClientStateListener = new BillingClientStateListener();
            var priceChangeService = new GooglePriceChangeService(googleBillingClient, googleQuerySkuDetailsService);
            var telemetryDiagnostics = new TelemetryDiagnostics(telemetryDiagnosticsInstanceWrapper);
            var telemetryMetrics = new TelemetryMetricsService(telemetryMetricsInstanceWrapper);

            googlePurchaseUpdatedListener.SetGoogleQueryPurchaseService(queryPurchasesService);

            var googlePlayStoreService = new MetricizedGooglePlayStoreService(
                googleBillingClient,
                googleQuerySkuDetailsService,
                purchaseService,
                finishTransactionService,
                queryPurchasesService,
                billingClientStateListener,
                priceChangeService,
                googleLastKnownProductService,
                telemetryDiagnostics,
                telemetryMetrics,
                logger
            );

            googlePlayStoreService.InitConnectionWithGooglePlay();

            return googlePlayStoreService;
        }

        private IStore InstantiateUDP()
        {
            var store = new UDPImpl();
            BindExtension<IUDPExtensions>(store);
            var nativeUdpStore = (INativeUDPStore)GetAndroidNativeStore(store);
            store.SetNativeStore(nativeUdpStore);
            return store;
        }

        private IStore InstantiateAndroidHelper(JSONStore store)
        {
            store.SetNativeStore(GetAndroidNativeStore(store));
            return store;
        }

        private INativeStore GetAndroidNativeStore(JSONStore store)
        {
            return m_NativeStoreProvider.GetAndroidStore(store, appStore, m_Binder, util);
        }

#if UNITY_PURCHASING_GPBL
        private IStore InstantiateGooglePlayBilling()
        {
            var gameObject = new GameObject("GooglePlayBillingUtil");
            Object.DontDestroyOnLoad (gameObject);
            gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

            var _util = gameObject.AddComponent<GooglePlayBillingUtil>();

            var store = new GooglePlayStoreImpl(_util);
            BindExtension((IGooglePlayStoreExtensions) store);
            BindConfiguration((IGooglePlayConfiguration) store);
            return store;
        }
#endif

        private IStore InstantiateApple()
        {
            var telemetryDiagnostics = new TelemetryDiagnostics(telemetryDiagnosticsInstanceWrapper);
            var telemetryMetrics = new TelemetryMetricsService(telemetryMetricsInstanceWrapper);
            var store = new MetricizedAppleStoreImpl(util, telemetryDiagnostics, telemetryMetrics);
            var appleBindings = m_NativeStoreProvider.GetStorekit(store);
            store.SetNativeStore(appleBindings);
            BindExtension<IAppleExtensions>(store);
            return store;
        }

        private WinRTStore windowsStore;

        private void UseMockWindowsStore(bool value)
        {
            if (null != windowsStore)
            {
                var iap = Default.Factory.Create(value);
                windowsStore.SetWindowsIAP(iap);
            }
        }

        private IStore instantiateWindowsStore()
        {
            // Create a non mocked store by default.
            var iap = Default.Factory.Create(false);
            windowsStore = new WinRTStore(iap, util, logger);
            // Microsoft require polling for new purchases on each app foregrounding.
            util.AddPauseListener(windowsStore.restoreTransactions);
            return windowsStore;
        }

        private IStore InstantiateFakeStore()
        {
            FakeStore fakeStore = null;
            if (useFakeStoreUIMode != FakeStoreUIMode.Default)
            {
                // To access class not available due to UnityEngine.UI conflicts with
                // unit-testing framework, instantiate via reflection
                fakeStore = new UIFakeStore
                {
                    UIMode = useFakeStoreUIMode
                };
            }

            if (fakeStore == null)
            {
                fakeStore = new FakeStore();
            }
            return fakeStore;
        }

        /// <summary>
        /// The MicrosoftConfiguration is used to toggle between simulated
        /// and live IAP implementations.
        /// The switching is done in the StandardPurchasingModule,
        /// but we don't want the to implement IMicrosoftConfiguration since
        /// we want that implementation to be private and the module is public.
        /// </summary>
        private class MicrosoftConfiguration : IMicrosoftConfiguration
        {
            public MicrosoftConfiguration(StandardPurchasingModule module)
            {
                this.module = module;
            }
            private bool useMock;
            private readonly StandardPurchasingModule module;

            public bool useMockBillingSystem
            {
                get => useMock;

                set
                {
                    module.UseMockWindowsStore(value);
                    useMock = value;
                }
            }
        }
    }
}
