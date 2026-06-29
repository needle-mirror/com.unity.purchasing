using System.Collections.Generic;
using System.Threading.Tasks;
#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED || IAP_UNITY_AUTH_ENABLED
using Unity.Services.Authentication;
#endif
using Unity.Services.Authentication.Internal;
using Unity.Services.Core;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Telemetry.Internal;
using UnityEngine.Purchasing.Stores;
using UnityEngine.Purchasing.Stores.Data.Insights;
using UnityEngine.Purchasing.Telemetry;
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.WebshopService;

namespace UnityEngine.Purchasing.Registration
{
    class IapCoreInitializeCallback : IInitializablePackage
    {
        const string k_PurchasingPackageName = "com.unity.purchasing";
        static SessionEventEmitter s_SessionEventEmitter;
        static DeepLinkService s_DeepLinkService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Clear the cached handle on (re)load so a Disable-Domain-Reload session re-subscribes
            // to the fresh DeepLinkService (DeepLinkManager disposes the old one) instead of a stale,
            // disposed instance that would silently stop handling deep links.
            s_DeepLinkService = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            CoreRegistry.Instance.RegisterPackage(new IapCoreInitializeCallback())
                .DependsOn<IMetricsFactory>()
                .DependsOn<IDiagnosticsFactory>()
                .DependsOn<ICloudProjectId>()
                .DependsOn<IEngineInstallationId>()
                .OptionallyDependsOn<IProjectConfiguration>()
                .OptionallyDependsOn<IAccessToken>()
                .OptionallyDependsOn<IEnvironmentId>()
                .OptionallyDependsOn<IAnalyticsStandardEventComponent>()
                .OptionallyDependsOn<ITargetingActivation>()
                ;
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

            InitializePaymentProviderService(registry);
            InitializeLiveContentAdapterService(registry);
            InitializeWebshopService(registry);

            InitializeSessionEvents();
            InitializeDeepLinkService(registry);

            return Task.CompletedTask;
        }

        static void InitializeSessionEvents()
        {
#if IAP_UNITY_AUTH_ENABLED
            if (s_SessionEventEmitter != null) return; // idempotent — Initialize can re-run

            var playerData = new PlayerData(
                new CoreRegistryHelper(),
                UnityUtilContainer.Instance(),
                StoreFactory.Instance().StoreLocationContext);
            s_SessionEventEmitter = new SessionEventEmitter(playerData, new CoreRegistryHelper());

            AuthenticationService.Instance.SignedIn += s_SessionEventEmitter.SendAuthenticationCompleteEvent;
#endif
        }

        private void InitializeLiveContentAdapterService(CoreRegistry registry)
        {
            var environmentId = registry.GetServiceComponent<IEnvironmentId>();
            var cloudProjectId = registry.GetServiceComponent<ICloudProjectId>();
            var accessToken = registry.GetServiceComponent<IAccessToken>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();

            var host = GetLiveContentHost(projectConfiguration);

            LiveContentAdapterServiceProvider.Instance().CreateLiveContentAdapterService(accessToken, environmentId, cloudProjectId, host);
        }

        string GetLiveContentHost(IProjectConfiguration projectConfiguration)
        {
            var cloudEnvironment = projectConfiguration?.GetString(k_CloudEnvironmentKey);

            switch (cloudEnvironment)
            {
                case k_StagingEnvironment:
                    return "https://staging.services.api.unity.com/live-content/client/v1";
                default:
                    return "https://services.api.unity.com/live-content/client/v1";
            }
        }

        private void InitializePaymentProviderService(CoreRegistry registry)
        {
            // TODO deal with key not found exception here
            var environmentId = registry.GetServiceComponent<IEnvironmentId>();
            var cloudProjectId = registry.GetServiceComponent<ICloudProjectId>();
            var accessToken = registry.GetServiceComponent<IAccessToken>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();

            var host = GetHost(projectConfiguration);

#if IAP_AUTH_TARGETING_ENABLED
            if (registry.TryGetServiceComponent(out ITargetingActivation targeting))
            {
                targeting.RegisterConsumer(k_PurchasingPackageName);
            }
#endif

            PaymentProviderServiceProvider.Instance().CreatePaymentProviderService(accessToken, environmentId, cloudProjectId, host);
        }

        private void InitializeWebshopService(CoreRegistry registry)
        {
            var environmentId = registry.GetServiceComponent<IEnvironmentId>();
            var cloudProjectId = registry.GetServiceComponent<ICloudProjectId>();
            var accessToken = registry.GetServiceComponent<IAccessToken>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();

            var host = GetWebshopHost(projectConfiguration);

            WebshopServiceProvider.Instance().CreateWebshopService(accessToken, environmentId, cloudProjectId, host);
        }

        private void InitializeDeepLinkService(CoreRegistry registry)
        {
            if (s_DeepLinkService == null)
            {
                s_DeepLinkService = (DeepLinkService)DeepLinkManager.GetDeepLinkService();
                s_DeepLinkService.OnDeepLinkActivated += link => _ = OnDeepLinkActivated(link);
            }
        }

        // The Webshop appends this keyword to the deep link it invokes for an auth-gated
        // redirect. We react ONLY to that link — any other deep link (including the Webshop
        // sending the player back into the game for promotions) must not trigger a redirect,
        // or we'd bounce the player straight back to the Webshop.
        const string k_WebshopDeepLinkKeyword = "frictionless-webshop";

        static async Task OnDeepLinkActivated(string link)
        {
            if (!IsWebshopDeepLink(link))
            {
                return;
            }

            await UnityServices.InitializeAsync();

#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                s_DeepLinkService?.RaiseAuthenticationFailed();
                if (!await WaitForSignInAsync())
                {
                    return;
                }
            }

            var storeController = new StoreController(PaymentProvider.Name);
            // Warm start: if the store is already connected, OnStoreConnected has already
            // fired, so redirect now; otherwise wait for the connection event.
            if (storeController.GetConnectionState() == ConnectionState.Connected)
            {
                OnStoreConnected();
            }
            else
            {
                storeController.OnStoreConnected += OnStoreConnected;
            }
#else
            Debug.unityLogger.LogIAPError("Authentication 3.7.1 package is not present, unable to redirect to Webshop");
#endif
        }

        static bool IsWebshopDeepLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return false;
            }

            // Keyword must appear in the query portion (after '?'), matching the
            // deep link the Webshop invokes, e.g. myapp://open?frictionless-webshop.
            var queryIndex = link.IndexOf('?');
            return queryIndex >= 0
                && link.IndexOf(k_WebshopDeepLinkKeyword, queryIndex, System.StringComparison.Ordinal) >= 0;
        }

#if IAP_UNITY_AUTH_RESTRICTED_TOKEN_ENABLED
        static void OnStoreConnected()
        {
            // One-shot: drop the subscription so we don't re-fire on every future (re)connect
            // or stack subscriptions across repeated deep links. Forwards to the singleton
            // StoreService; a no-op on the warm-start (direct-call) path.
            new StoreController(PaymentProvider.Name).OnStoreConnected -= OnStoreConnected;
            if (UnityIAPServices.Purchase(PaymentProvider.Name).PaymentProviders is { } paymentProviders)
            {
                paymentProviders.RedirectToWebshop();
            }
        }

        static async Task<bool> WaitForSignInAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            void OnSignedIn() => tcs.TrySetResult(true);
            void OnSignInFailed(RequestFailedException _) => tcs.TrySetResult(false);
            AuthenticationService.Instance.SignedIn += OnSignedIn;
            AuthenticationService.Instance.SignInFailed += OnSignInFailed;
            try
            {
                return await tcs.Task;
            }
            finally
            {
                AuthenticationService.Instance.SignedIn -= OnSignedIn;
                AuthenticationService.Instance.SignInFailed -= OnSignInFailed;
            }
        }
#endif

        string GetWebshopHost(IProjectConfiguration projectConfiguration)
        {
            var cloudEnvironment = projectConfiguration?.GetString(k_CloudEnvironmentKey);

            switch (cloudEnvironment)
            {
                case k_StagingEnvironment:
                    return "https://webshop-stg.services.api.unity.com";
                default:
                    return "https://webshop.services.api.unity.com";
            }
        }

        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";
        const string k_StagingEnvironment = "staging";

        string GetHost(IProjectConfiguration projectConfiguration)
        {
            var cloudEnvironment = projectConfiguration?.GetString(k_CloudEnvironmentKey);

            switch (cloudEnvironment)
            {
                case k_StagingEnvironment:
                    return "https://iap-stg.services.api.unity.com";
                default:
                    return "https://iap.services.api.unity.com";
            }
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
