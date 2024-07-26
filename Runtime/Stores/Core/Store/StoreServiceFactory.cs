#nullable enable
using System;
using System.Collections.Generic;
using Uniject;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Services;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.UseCases;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing
{
    public class StoreServiceFactory : IStoreServiceFactory
    {
        static StoreServiceFactory? s_Instance;

        readonly Dictionary<string?, Func<IRetryPolicy, IStoreWrapper, IStoreService>> m_StoreServiceInstantiationByName = new Dictionary<string?, Func<IRetryPolicy, IStoreWrapper, IStoreService>>();

        internal static StoreServiceFactory Instance()
        {
            return s_Instance ??= new StoreServiceFactory();
        }

        StoreServiceFactory()
        {
            m_StoreServiceInstantiationByName.Add(GooglePlay.Name, CreateGoogleStoreService);
            m_StoreServiceInstantiationByName.Add(AppleAppStore.Name, CreateAppleStoreService);
            m_StoreServiceInstantiationByName.Add(MacAppStore.Name, CreateAppleStoreService);
            m_StoreServiceInstantiationByName.Add(AmazonApps.Name, CreateAmazonStoreService);
        }

        public void RegisterNewService(string name, Func<IStoreService> createFunction)
        {
            IStoreService CreateCustomStoreServiceDiscardParams(IRetryPolicy retryPolicy, IStoreWrapper storeWrapper)
            {
                return createFunction?.Invoke() ?? throw new InvalidOperationException();
            }

            m_StoreServiceInstantiationByName.Add(name, CreateCustomStoreServiceDiscardParams);
        }

        public void RegisterNewExtendedService(string name, Func<IStoreService, ExtensibleStoreService> createFunction)
        {
            IStoreService CreateCustomStoreServiceDiscardParams(IRetryPolicy retryPolicy, IStoreWrapper storeWrapper)
            {
                return createFunction?.Invoke(CreateGenericStoreService(retryPolicy, storeWrapper)) ?? throw new InvalidOperationException();
            }

            m_StoreServiceInstantiationByName.Add(name, CreateCustomStoreServiceDiscardParams);
        }

        public IStoreService Create(IStoreWrapper store, IRetryPolicy? retryPolicy)
        {
            return (m_StoreServiceInstantiationByName.ContainsKey(store.name) ? m_StoreServiceInstantiationByName[store.name]?.Invoke(retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy)), store) : CreateGenericStoreService(retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy)), store)) ?? throw new InvalidOperationException();
        }

        static StoreService CreateGenericStoreService(IRetryPolicy retryPolicy, IStoreWrapper storeWrapper)
        {
            IDependencyInjectionService di = new DependencyInjectionService();
            AddStoreServiceDependencies(di, retryPolicy, storeWrapper);
            return di.GetInstance<StoreService>();
        }

        static void AddStoreServiceDependencies(IDependencyInjectionService di, IRetryPolicy? retryPolicy, IStoreWrapper store)
        {
            di.AddInstance(UnityUtilContainer.Instance());
            di.AddService<RetryService>();
            var storeConnectUseCase = new StoreConnectUseCaseFactory().CreateUseCase(store, di.GetInstance<IRetryService>());
            di.AddInstance(storeConnectUseCase);
            if (retryPolicy != null)
            {
                di.AddInstance(retryPolicy);
            }

            di.AddService<StoreService>();
        }

        static GooglePlayStoreExtendedService CreateGoogleStoreService(IRetryPolicy? retryPolicy, IStoreWrapper store)
        {
            IDependencyInjectionService di = new DependencyInjectionService();

            di.AddInstance(Debug.unityLogger);
            di.AddService<GoogleCachedQueryProductDetailsService>();
            di.AddService<GooglePurchaseBuilder>();
            di.AddService<GooglePurchasesUpdatedListener>();
            di.AddInstance(StoreFactory.Instance().TelemetryDiagnosticsInstanceWrapper);
            di.AddService<TelemetryDiagnostics>();
            di.AddService<GoogleBillingClient>();
            di.AddService<GooglePlayStoreSetObfuscatedIdUseCase>();
            di.AddService<GoogleQueryPurchasesUseCase>();

            AddStoreServiceDependencies(di, retryPolicy, store);
            di.AddService<GooglePlayStoreExtendedService>();

            return di.GetInstance<GooglePlayStoreExtendedService>();
        }

        static AppleStoreExtendedService CreateAppleStoreService(IRetryPolicy? retryPolicy, IStoreWrapper store)
        {
            var appleNativeStore = (store.instance as AppleStoreImpl)?.GetNativeStore();
            if (appleNativeStore == null)
            {
                throw new Exception("AppleStoreImpl's INativeAppleStore has not been set");
            }

            IDependencyInjectionService di = new DependencyInjectionService();

            di.AddInstance(appleNativeStore);
            di.AddInstance((AppleStoreImpl)store.instance);
            di.AddService<CanMakePaymentsUseCase>();
            di.AddService<SetApplicationUsernameUseCase>();
            di.AddService<ClearAppleTransactionLogsUseCase>();

            AddStoreServiceDependencies(di, retryPolicy, store);
            di.AddService<AppleStoreExtendedService>();

            return di.GetInstance<AppleStoreExtendedService>();
        }

        static AmazonAppsStoreExtendedService CreateAmazonStoreService(IRetryPolicy? retryPolicy, IStoreWrapper store)
        {
            IDependencyInjectionService di = new DependencyInjectionService();

            AddStoreServiceDependencies(di, retryPolicy, store);
            di.AddService<AmazonStoreCartValidator>();
            di.AddInstance(Debug.unityLogger);
            di.AddInstance(AmazonApps.Name);
            di.AddInstance(StoreFactory.Instance().TelemetryMetricsInstanceWrapper);
            di.AddService<MetricizedJsonStore>();

            IUnityCallback callback = di.GetInstance<MetricizedJsonStore>();
            var util = di.GetInstance<IUtil>();
            var amazonJavaStore = new AmazonNativeStoreBuilder().GetAmazonStore(callback, util);
            di.AddInstance(callback);
            di.AddInstance(amazonJavaStore);

            di.AddService<AmazonAppsStoreExtendedService>();

            return di.GetInstance<AmazonAppsStoreExtendedService>();
        }
    }
}
