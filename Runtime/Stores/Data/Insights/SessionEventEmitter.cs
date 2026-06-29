#nullable enable

using System;
using UnityEngine.Purchasing.Registration;
using UnityEngine.Purchasing.Stores.Data.Insights.Models;

namespace UnityEngine.Purchasing.Stores.Data.Insights
{
    // Emits session-level Insights events that are not tied to a purchase
    // journey or a specific store (e.g. AuthenticationCompleteEvent). The
    // envelope carries identity / device / consent context but no Order and
    // no Store; the proto-faithful builder skips both via the standard
    // default-value rules.
    //
    // Sibling to PurchaseEventEmitter: reuses its static helpers
    // (BuildSdkDeviceInfo, MapDeviceInfo, MapIdentity, ParseConsent, Forward)
    // to avoid duplicating envelope plumbing.
    internal sealed class SessionEventEmitter
    {
        readonly IPlayerData m_PlayerData;
        readonly ICoreRegistryHelper m_CoreRegistry;

        internal SessionEventEmitter(IPlayerData playerData, ICoreRegistryHelper coreRegistry)
        {
            m_PlayerData = playerData;
            m_CoreRegistry = coreRegistry;

            // Pre-warm install timestamp cache on the main thread for the same
            // reason PurchaseEventEmitter does (the BuildEnvelope await can
            // resume on a background thread where Unity APIs would throw).
            _ = AppInstallInfo.GetInstallTimestamp();
        }

        // Fire-and-forget: telemetry must never break the auth flow, so the
        // entire body is guarded and emission failures are logged at verbose.
        public async void SendAuthenticationCompleteEvent()
        {
            try
            {
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                var iaps = BuildEnvelope(pps, new AuthenticationCompleteEvent());
                PurchaseEventEmitter.Forward(iaps);
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPVerbose(
                    $"Insights emission failed for {nameof(AuthenticationCompleteEvent)}: {e.Message}");
            }
        }

        IAPSDKEvent BuildEnvelope(PaymentProviderService.Models.PlayerIdentity pps, IEventVariant variant)
        {
            var sdkDeviceInfo = PurchaseEventEmitter.BuildSdkDeviceInfo(Application.platform);
            return new IAPSDKEvent
            {
                EventUuid = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                SessionId = pps.SessionId,
                FirebaseSessionId = pps.UnityFirebaseSessionId,
                FirebaseAppId = pps.UnityFirebaseAppId,
                ProjectId = m_CoreRegistry.CloudProjectId ?? "",
                EnvironmentId = m_CoreRegistry.EnvironmentId ?? "",
                IapSdkVersion = IAPVersion.Current,
                EngineVersion = Application.unityVersion,
                UnityConsentStateAdsIntent = PurchaseEventEmitter.ParseConsent(pps.UnityConsentStateAdsIntent),
                UnityConsentStateAnalyticsIntent = PurchaseEventEmitter.ParseConsent(pps.UnityConsentStateAnalyticsIntent),
                UnityIdentities = PurchaseEventEmitter.MapIdentity(pps, m_CoreRegistry),
                DeviceInfo = PurchaseEventEmitter.MapDeviceInfo(sdkDeviceInfo),
                Reporting = new Reporting
                {
                    Platform = sdkDeviceInfo?.Platform ?? "",
                    AppBundleId = sdkDeviceInfo?.AppBundleID ?? Application.identifier
                },
                // No Store / Order — session-level event, not store-scoped.
                EventData = variant,
                ApplicationVersion = Application.version,
                InstallationTimestamp = AppInstallInfo.GetInstallTimestamp(),
            };
        }
    }
}
