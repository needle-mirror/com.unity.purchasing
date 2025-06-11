using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Configuration.Internal;
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
                .DependsOn<IDiagnosticsFactory>()
                .DependsOn<ICloudProjectId>()
                .OptionallyDependsOn<IEnvironmentId>()
                .OptionallyDependsOn<IAnalyticsStandardEventComponent>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            var metricsInstanceWrapper = StoreFactory.Instance().TelemetryMetricsInstanceWrapper;
            var diagnosticsInstanceWrapper = StoreFactory.Instance().TelemetryDiagnosticsInstanceWrapper;

            ITelemetryMetricsService telemetryMetricsService = new TelemetryMetricsService(metricsInstanceWrapper);
            telemetryMetricsService.ExecuteTimedAction(
                () =>
                {
                    CacheInitializedEnvironment(registry);
                    InitializeTelemetryComponents(metricsInstanceWrapper, diagnosticsInstanceWrapper);
                },
                TelemetryMetricDefinitions.packageInitTimeName
            );

            return Task.CompletedTask;
        }

        static void CacheInitializedEnvironment(CoreRegistry registry)
        {
            var currentEnvironment = GetCurrentEnvironment(registry);
            CoreServicesEnvironmentSubject.Instance().UpdateCurrentEnvironment(currentEnvironment);
        }

        static string GetCurrentEnvironment(CoreRegistry registry)
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
