using System.Threading.Tasks;
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
            var metricsInstanceWrapper = StandardPurchasingModule.Instance().telemetryMetricsInstanceWrapper;

            ITelemetryMetrics telemetryMetrics = new TelemetryMetrics(metricsInstanceWrapper);
            var packageInitTimeMetric = telemetryMetrics.CreateAndStartMetricEvent(TelemetryMetricTypes.Histogram, TelemetryMetricNames.packageInitTimeName);

            var diagnosticsInstanceWrapper = StandardPurchasingModule.Instance().telemetryDiagnosticsInstanceWrapper;
            var diagnosticsFactory = CoreRegistry.Instance.GetServiceComponent<IDiagnosticsFactory>();
            diagnosticsInstanceWrapper.SetDiagnosticsInstance(diagnosticsFactory.Create(k_PurchasingPackageName));

            var metricsFactory = CoreRegistry.Instance.GetServiceComponent<IMetricsFactory>();
            metricsInstanceWrapper.SetMetricsInstance(metricsFactory.Create(k_PurchasingPackageName));

            packageInitTimeMetric.StopAndSendMetric();

            return Task.CompletedTask;
        }
    }
}
