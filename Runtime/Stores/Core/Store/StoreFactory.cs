#nullable enable

using System;
using System.Collections.Generic;
using Purchasing.Extension;
using Stores.Android.GooglePlay.AAR.Interfaces;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing
{
    class StoreFactory : IStoreFactory
    {
        static StoreFactory? s_Instance;

        readonly IUtil m_Util;
        readonly ILogger m_Logger;
        readonly INativeStoreProvider m_NativeStoreProvider;
        public readonly ITelemetryMetricsInstanceWrapper TelemetryMetricsInstanceWrapper;
        public readonly ITelemetryDiagnosticsInstanceWrapper TelemetryDiagnosticsInstanceWrapper;
        readonly Dictionary<string?, Func<IStoreWrapper>> m_StoreInstantiationByName = new Dictionary<string?, Func<IStoreWrapper>>();

        internal StoreFactory(IUtil util, ILogger logger, INativeStoreProvider nativeStoreProvider, ITelemetryDiagnosticsInstanceWrapper telemetryDiagnosticsInstanceWrapper, ITelemetryMetricsInstanceWrapper telemetryMetricsInstanceWrapper)
        {
            m_Util = util;
            m_Logger = logger;
            m_NativeStoreProvider = nativeStoreProvider;
            TelemetryDiagnosticsInstanceWrapper = telemetryDiagnosticsInstanceWrapper;
            TelemetryMetricsInstanceWrapper = telemetryMetricsInstanceWrapper;
            RegisterBaseStores();
        }

        internal static StoreFactory Instance()
        {
            if (s_Instance == null)
            {
                var logger = Debug.unityLogger;
                var util = UnityUtilContainer.Instance();
                var amazonStoreFactory = new AmazonNativeStoreBuilder();
                var nativeStoreProvider = new NativeStoreProvider(amazonStoreFactory);
                s_Instance = new StoreFactory(util, logger, nativeStoreProvider,
                    new TelemetryDiagnosticsInstanceWrapper(logger, util),
                    new TelemetryMetricsInstanceWrapper(logger, util));
            }

            return s_Instance;
        }

        void RegisterBaseStores()
        {
            RegisterStore(AmazonApps.Name, InstantiateAmazonStore);
            RegisterStore(AppleAppStore.Name, InstantiateAppleStore);
            RegisterStore(FakeAppStore.Name, InstantiateFakeStore);
            RegisterStore(MacAppStore.Name, InstantiateMacAppStore);
            RegisterStore(GooglePlay.Name, InstantiateGooglePlayStore);
        }

        public void RegisterStore(string? storeName, Func<IStoreWrapper> function)
        {
            m_StoreInstantiationByName[storeName] = function;
        }

        public IStoreWrapper CreateStore(string? storeName)
        {
            try
            {
                return m_StoreInstantiationByName[storeName]();
            }
            catch (Exception ex)
            {
                throw new StoreException($"An error has occured when attempting to create the store: {storeName}.", ex);
            }
        }

        IStoreWrapper InstantiateAmazonStore()
        {
            var di = CreateBaseDiService();

            di.AddService<AmazonStoreCartValidator>();
            di.AddService<MetricizedJsonStore>();
            di.AddInstance(AmazonApps.Name);

            var nativeStore = CreateAndAssignNativeAndroidStore(di.GetInstance<JsonStore>());

            return new StoreWrapper(AmazonApps.Name, di.GetInstance<InternalStore>());
        }

        IDependencyInjectionService CreateBaseDiService()
        {
            var di = new DependencyInjectionService();
            AddUtilsDependencies(di);
            AddTelemetryDependencies(di);
            return di;
        }

        void AddUtilsDependencies(IDependencyInjectionService di)
        {
            di.AddInstance(m_Util);
            di.AddInstance(m_Logger);
        }

        void AddTelemetryDependencies(IDependencyInjectionService di)
        {
            di.AddInstance(TelemetryMetricsInstanceWrapper);
            di.AddInstance(TelemetryDiagnosticsInstanceWrapper);
            di.AddService<TelemetryDiagnostics>();
            di.AddService<TelemetryMetricsService>();
        }

        IAndroidJavaStore CreateAndAssignNativeAndroidStore(JsonStore store)
        {
            var nativeStore = m_NativeStoreProvider.GetAmazonStore(store, m_Util);
            store.SetNativeStore(nativeStore);
            return nativeStore;
        }

        IStoreWrapper InstantiateAppleStore()
        {
            return InstantiateAppleAppStore(AppleAppStore.Name, AppleAppStore.DisplayName);
        }

        IStoreWrapper InstantiateMacAppStore()
        {
            return InstantiateAppleAppStore(MacAppStore.Name, MacAppStore.DisplayName);
        }

        IStoreWrapper InstantiateAppleAppStore(string? storeName, string storeDisplayName)
        {
            var di = CreateBaseDiService();
            di.AddInstance(new AppleAppStoreCartValidator(storeDisplayName));
            AddMetricizedAppleStoreDependencies(di);
            CreateAndAssignNativeAppleStore(di.GetInstance<AppleStoreImpl>());
            return CreateStoreWrapper(storeName, di);
        }

        void AddMetricizedAppleStoreDependencies(IDependencyInjectionService di)
        {
            di.AddService<AppleRetrieveProductsService>();
            di.AddService<MetricizedAppleStoreImpl>();
            di.AddInstance(BuildTransactionLog(di));
        }

        static ITransactionLog BuildTransactionLog(IDependencyInjectionService di)
        {
            return new TransactionLog(di.GetInstance<ILogger>(), Application.persistentDataPath);
        }

        void CreateAndAssignNativeAppleStore(AppleStoreImpl store)
        {
            var appleBindings = m_NativeStoreProvider.GetStorekit(store);
            store.SetNativeStore(appleBindings);
        }

        IStoreWrapper InstantiateFakeStore()
        {
            var fakeStore = CreateFakeStoreByUIMode(UseFakeStoreUIMode());
            return new StoreWrapper(FakeAppStore.Name, fakeStore);
        }

        static FakeStoreUIMode UseFakeStoreUIMode()
        {
            // TODO: IAP-2795 - Add a way to configure this
            // Setting to StandardUser in order to emulate the Codeless IAP standard.
            return FakeStoreUIMode.StandardUser;
        }

        FakeStore CreateFakeStoreByUIMode(FakeStoreUIMode useFakeStoreUIMode)
        {
            var cartValidator = new FakeStoreCartValidator();
            if (useFakeStoreUIMode != FakeStoreUIMode.Default)
            {
                // To access class not available due to UnityEngine.UI conflicts with
                // unit-testing framework, instantiate via reflection
                return new UIFakeStore(cartValidator, m_Logger) { UIMode = useFakeStoreUIMode };
            }

            return new FakeStore(cartValidator, m_Logger);
        }

        IStoreWrapper InstantiateGooglePlayStore()
        {
            var di = CreateBaseDiService();
            AddGooglePlayStoreServices(di);
            AddGooglePlayStoreServiceAars(di);
            LinkGooglePlayStoreDependencies(di);

            return CreateStoreWrapper(GooglePlay.Name, di);
        }

        static IStoreWrapper CreateStoreWrapper(string? storeName, IDependencyInjectionService di)
        {
            return new StoreWrapper(storeName, di.GetInstance<InternalStore>());
        }

        void LinkGooglePlayStoreDependencies(IDependencyInjectionService di)
        {
            di.GetInstance<GooglePurchasesUpdatedHandler>().SubscribeToPurchasesUpdatedEvent(di.GetInstance<GooglePurchasesUpdatedListener>());
            LinkProductCache(di);
            m_Util.AddPauseListener(di.GetInstance<IGooglePlayStore>().OnPause);
        }

        static void LinkProductCache(IDependencyInjectionService di)
        {
            var productCache = di.GetInstance<GooglePlayStore>().ProductCache;
            di.GetInstance<IGooglePlayStoreFetchPurchasesService>().SetProductCache(productCache);
            di.GetInstance<IGooglePlayStoreFinishTransactionService>().SetProductCache(productCache);
            di.GetInstance<IGooglePurchaseService>().SetProductCache(productCache);
            di.GetInstance<IGooglePurchasesUpdatedHandler>().SetProductCache(productCache);
            di.GetInstance<GooglePlayPurchaseCallback>().SetProductCache(productCache);
        }

        static void AddGooglePlayStoreServices(IDependencyInjectionService di)
        {
            di.AddService<ProductDetailsConverter>();
            di.AddService<GooglePurchaseConverter>();
            di.AddService<GooglePlayPurchaseCallback>();
            di.AddService<GooglePlayStorePurchaseService>();
            di.AddService<GooglePlayStoreFinishTransactionService>();
            di.AddService<GooglePlayStoreFetchPurchasesService>();
            di.AddService<TelemetryMetricsInstanceWrapper>();
            di.AddService<GooglePlayStoreRetrieveProductsService>();
            di.AddService<GooglePlayStoreCheckEntitlementService>();
            di.AddService<GooglePlayStoreChangeSubscriptionService>();
            di.AddService<GooglePlayCartValidator>();
            di.AddService<GooglePlayStore>();
        }

        void AddGooglePlayStoreServiceAars(IDependencyInjectionService di)
        {
            di.AddService<GoogleLastKnownProductService>();
            di.AddService<GooglePurchaseStateEnumProvider>();
            di.AddService<GooglePurchaseBuilder>();
            di.AddService<GooglePurchasesUpdatedListener>();
            di.AddService<GoogleBillingClient>();
            di.AddService<GoogleCachedQueryProductDetailsService>();
            di.AddService<QueryProductDetailsService>();
            di.AddService<GooglePurchaseService>();
            di.AddService<GoogleQueryPurchasesUseCase>();
            di.AddService<GoogleFinishTransactionUseCase>();
            di.AddService<GooglePlayCheckEntitlementUseCase>();
            di.AddService<BillingClientStateListener>();
            di.AddService<GooglePlayStoreConnectionService>();
            di.AddService<GooglePurchasesUpdatedHandler>();
            di.AddService<MetricizedGooglePlayStoreService>();
        }
    }
}
