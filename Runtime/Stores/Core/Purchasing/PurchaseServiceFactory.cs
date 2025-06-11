#nullable enable
using System;
using System.Collections.Generic;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Internal;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Services;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.UseCases;
#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
using Unity.Services.Analytics;
#endif

namespace UnityEngine.Purchasing
{
    class PurchaseServiceFactory : IPurchaseServiceFactory
    {
        static PurchaseServiceFactory? s_Instance;

        readonly Dictionary<string?, Func<IStoreWrapper, IPurchaseService>> m_PurchaseServiceInstantiationByName = new Dictionary<string?, Func<IStoreWrapper, IPurchaseService>>();

        internal static PurchaseServiceFactory Instance()
        {
            return s_Instance ??= new PurchaseServiceFactory();
        }

        PurchaseServiceFactory()
        {
            m_PurchaseServiceInstantiationByName.Add(GooglePlay.Name, CreateGooglePurchaseService);
            m_PurchaseServiceInstantiationByName.Add(AppleAppStore.Name, CreateApplePurchaseService);
            m_PurchaseServiceInstantiationByName.Add(MacAppStore.Name, CreateApplePurchaseService);
        }

        public void RegisterNewService(string name, Func<IPurchaseService> createFunction)
        {
            IPurchaseService CreateCustomProductServiceDiscardParams(IStoreWrapper storeWrapper)
            {
                return createFunction?.Invoke() ?? throw new InvalidOperationException();
            }

            m_PurchaseServiceInstantiationByName.Add(name, CreateCustomProductServiceDiscardParams);
        }

        public void RegisterNewExtendedService(string name, Func<IPurchaseService, ExtensiblePurchaseService> createFunction)
        {
            IPurchaseService CreateCustomProductServiceDiscardParams(IStoreWrapper storeWrapper)
            {
                return createFunction?.Invoke(CreateDefaultPurchaseService(storeWrapper)) ?? throw new InvalidOperationException();
            }

            m_PurchaseServiceInstantiationByName.Add(name, CreateCustomProductServiceDiscardParams);
        }

        public IPurchaseService Create(IStoreWrapper store)
        {
            return ((m_PurchaseServiceInstantiationByName.ContainsKey(store.name)) ? m_PurchaseServiceInstantiationByName[store.name]?.Invoke(store) : CreateDefaultPurchaseService(store)) ?? throw new InvalidOperationException();
        }

        static PurchaseService CreateDefaultPurchaseService(IStoreWrapper store)
        {
            IDependencyInjectionService di = new DependencyInjectionService();
            AddPurchaseServiceDependencies(store, di);
            di.AddService<PurchaseService>();
            return di.GetInstance<PurchaseService>();
        }

        static void AddPurchaseServiceDependencies(IStoreWrapper store, IDependencyInjectionService di)
        {
            di.AddInstance(store);
            di.AddInstance(store.instance);
            di.AddService<FetchPurchasesUseCase>();
            di.AddInstance(PurchaseUseCaseFactory.Create(store.instance, store.instance.ProductCache));
            di.AddService<ConfirmOrderUseCase>();
            di.AddService<CheckEntitlementUseCase>();
            di.AddService<OnEntitlementRevokedUseCase>();
            di.AddService<AnalyticsClient>();
            di.AddService<AppleRefreshAppReceiptUseCase>();

#if IAP_TX_VERIFIER_ENABLED
            string? projectId = null;
            string? environmentId = null;

            try
            {
                var registry = CoreRegistry.Instance;
                projectId = registry.GetServiceComponent<ICloudProjectId>().GetCloudProjectId();
                environmentId = registry.GetServiceComponent<IEnvironmentId>().EnvironmentId;
            }
            catch (Exception)
            {
                // The fields will return null, but TransactionVerifier handles it.
            }
            var transactionVerifier = new TransactionVerifier.TransactionVerifier(store.name, projectId, environmentId);
            di.AddInstance(transactionVerifier);
#endif

            di.AddInstance(Debug.unityLogger);
            AddAnalyticsDependencies(di);
        }

        static void AddAnalyticsDependencies(IDependencyInjectionService di)
        {
#if IAP_ANALYTICS_SERVICE_ENABLED && !DISABLE_RUNTIME_IAP_ANALYTICS
            di.AddService<AnalyticsServiceWrapper>();
            di.AddService<AnalyticsAdapter>();
#elif IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT && !DISABLE_RUNTIME_IAP_ANALYTICS
            di.AddService<AnalyticsServiceWrapper>();
            di.AddService<CoreAnalyticsAdapter>();
#else
            di.AddService<EmptyAnalyticsAdapter>();
#endif
        }

        static GooglePlayStoreExtendedPurchaseService CreateGooglePurchaseService(IStoreWrapper store)
        {
            IDependencyInjectionService di = new DependencyInjectionService();

            di.AddService<GooglePlayRestoreTransactionUseCase>();
            di.AddInstance(StoreFactory.Instance().TelemetryDiagnosticsInstanceWrapper);
            di.AddService<TelemetryDiagnostics>();

            AddPurchaseServiceDependencies(store, di);
            di.AddService<GooglePlayStoreExtendedPurchaseService>();

            return di.GetInstance<GooglePlayStoreExtendedPurchaseService>();
        }

        static AppleStoreExtendedPurchaseService CreateApplePurchaseService(IStoreWrapper store)
        {
            var appleNativeStore = (store.instance as AppleStoreImpl)?.GetNativeStore();
            if (appleNativeStore == null)
            {
                throw new Exception("AppleStoreImpl's INativeAppleStore has not been set");
            }

            IDependencyInjectionService di = new DependencyInjectionService();

            di.AddInstance(appleNativeStore);
            di.AddService<AppReceiptUseCase>();
            di.AddService<ContinuePromotionalPurchasesUseCase>();
            di.AddService<PresentCodeRedemptionSheetUseCase>();
            di.AddService<AppleRestoreTransactionsUseCase>();
            di.AddService<SetPromotionalPurchaseInterceptorCallbackUseCase>();
            di.AddService<SimulateAskToBuyUseCase>();

            AddPurchaseServiceDependencies(store, di);
            di.AddService<AppleStoreExtendedPurchaseService>();

            return di.GetInstance<AppleStoreExtendedPurchaseService>();
        }
    }
}
