using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Telemetry.Internal;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing.Registration
{
    class IapCoreInitializeCallback : IInitializablePackage
    {
        const string k_PurchasingPackageName = "com.unity.purchasing";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new IapCoreInitializeCallback())
                .DependsOn<IMetricsFactory>()
                .DependsOn<IDiagnosticsFactory>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            CacheInitializedEnvironment(registry);

            var metricsInstanceWrapper = StandardPurchasingModule.Instance().telemetryMetricsInstanceWrapper;
            var diagnosticsInstanceWrapper = StandardPurchasingModule.Instance().telemetryDiagnosticsInstanceWrapper;

            ITelemetryMetricsService telemetryMetricsService = new TelemetryMetricsService(metricsInstanceWrapper);
            telemetryMetricsService.ExecuteTimedAction(
                () => InitializeTelemetryComponents(metricsInstanceWrapper, diagnosticsInstanceWrapper),
                TelemetryMetricDefinitions.packageInitTimeName
            );

            return Task.CompletedTask;
        }

        void CacheInitializedEnvironment(CoreRegistry registry)
        {
            var currentEnvironment = GetCurrentEnvironment(registry);
            CoreServicesEnvironmentSubject.Instance().UpdateCurrentEnvironment(currentEnvironment);
        }

        string GetCurrentEnvironment(CoreRegistry registry)
        {
            try
            {
                return registry.GetServiceComponent<IEnvironments>().Current;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        static void InitializeTelemetryComponents(ITelemetryMetricsInstanceWrapper metricsInstanceWrapper,
            ITelemetryDiagnosticsInstanceWrapper diagnosticsInstanceWrapper)
        {
            var diagnosticsFactory = CoreRegistry.Instance.GetServiceComponent<IDiagnosticsFactory>();
            diagnosticsInstanceWrapper.SetDiagnosticsInstance(diagnosticsFactory.Create(k_PurchasingPackageName));

            var metricsFactory = CoreRegistry.Instance.GetServiceComponent<IMetricsFactory>();
            metricsInstanceWrapper.SetMetricsInstance(metricsFactory.Create(k_PurchasingPackageName));
        }
    }
}
