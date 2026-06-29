#nullable enable

using System;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device.Internal;
using Unity.Services.Core.Internal;

namespace UnityEngine.Purchasing.Registration
{
    // Surfaces the Core Services component values consumed by IAP without
    // requiring callers to know about CoreRegistry or the Core Services
    // interfaces. Each value is resolved lazily on first access so an
    // instance can be constructed before UnityServices.InitializeAsync
    // completes, and missing optional components (e.g. IPlayerId without
    // the Authentication package) yield null instead of throwing.
    internal interface ICoreRegistryHelper
    {
        string? InstallationId { get; }
        string? CloudProjectId { get; }
        string? EnvironmentId { get; }
        string? PlayerId { get; }
        string? ExternalUserId { get; }
    }

    internal sealed class CoreRegistryHelper : ICoreRegistryHelper
    {
        IEngineInstallationId? m_InstallationId;
        ICloudProjectId? m_CloudProjectId;
        IEnvironmentId? m_EnvironmentId;
        IPlayerId? m_PlayerId;
        IExternalUserId? m_ExternalUserId;

        public string? InstallationId => Resolve(ref m_InstallationId)?.GetOrCreateIdentifier();
        public string? CloudProjectId => Resolve(ref m_CloudProjectId)?.GetCloudProjectId();
        public string? EnvironmentId => Resolve(ref m_EnvironmentId)?.EnvironmentId;
        public string? PlayerId => Resolve(ref m_PlayerId)?.PlayerId;
        public string? ExternalUserId => Resolve(ref m_ExternalUserId)?.UserId;

        static T? Resolve<T>(ref T? cached) where T : class, IServiceComponent
        {
            if (cached != null) return cached;
            try
            {
                return cached = CoreRegistry.Instance.GetServiceComponent<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
