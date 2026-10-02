#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Registration;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Stores
{
    // Caches a link-out session id for the current player. All validity checks run on access.
    internal class LinkOutSessionIdProvider : ILinkOutSessionIdProvider
    {
        static readonly TimeSpan k_SessionLifetime = TimeSpan.FromHours(24);
        static readonly TimeSpan k_RefreshWindowBeforeExpiry = TimeSpan.FromHours(2);

        readonly ICoreRegistryHelper m_CoreRegistryHelper;
        readonly IPaymentProviderClientWrapper m_PaymentProviderClientWrapper;
        readonly ILogger m_Logger;
        readonly Func<DateTime> m_UtcNow;

        string? m_CachedId;
        string? m_CachedPlayerId;
        DateTime m_CachedIdCreatedAtUtc;
        // The cached id was registered while Firebase was unconfigured, so the next request
        // after configure replaces it. On macOS and visionOS Firebase is never configured, so
        // the id is never replaced this way.
        bool m_CachedIdHasUnconfiguredFirebaseIds;

        [Preserve]
        [Inject]
        internal LinkOutSessionIdProvider(
            ICoreRegistryHelper coreRegistryHelper,
            IPaymentProviderClientWrapper paymentProviderClientWrapper,
            ILogger logger)
            : this(coreRegistryHelper, paymentProviderClientWrapper, logger, () => DateTime.UtcNow)
        {
        }

        // Test seam.
        internal LinkOutSessionIdProvider(
            ICoreRegistryHelper coreRegistryHelper,
            IPaymentProviderClientWrapper paymentProviderClientWrapper,
            ILogger logger,
            Func<DateTime> utcNow)
        {
            m_CoreRegistryHelper = coreRegistryHelper;
            m_PaymentProviderClientWrapper = paymentProviderClientWrapper;
            m_Logger = logger;
            m_UtcNow = utcNow;
        }

        bool HasCachedId => m_CachedId != null;
        TimeSpan CachedIdAge => m_UtcNow() - m_CachedIdCreatedAtUtc;
        TimeSpan CachedIdTimeUntilExpiry => k_SessionLifetime - CachedIdAge;
        bool CachedIdHasExpired => CachedIdAge >= k_SessionLifetime;
        bool CachedIdAgeIsNegative => CachedIdAge < TimeSpan.Zero;

        public async Task<string?> GetLinkOutSessionId(
            IPlayerData playerData,
            Func<PaymentProviderService.Models.DeviceInfo?> deviceInfoFactory)
        {
            var playerId = m_CoreRegistryHelper.PlayerId;

            // The cached id has outlived its lifetime.
            if (HasCachedId && CachedIdHasExpired)
            {
                Clear();
            }

            // The cached id reports a negative age, so its real age is unknown.
            if (HasCachedId && CachedIdAgeIsNegative)
            {
                Clear();
            }

            // No player id to attribute an id to, so none is minted.
            // The cached id cannot be matched to a player, so it is not served.
            if (playerId == null)
            {
                return null;
            }

            // The cached id belongs to a different player.
            if (HasCachedId && m_CachedPlayerId != playerId)
            {
                Clear();
            }

            // The cached id was registered with Firebase ids from before the default app was
            // configured, and is returned if replacing it fails.
            if (HasCachedId && m_CachedIdHasUnconfiguredFirebaseIds && playerData.FirebaseDefaultAppConfigured)
            {
                return await FetchAndStoreWithNoRetries(playerId, playerData, deviceInfoFactory) ?? CachedIdIfPlayerUnchanged(playerId);
            }

            // The cached id is close enough to expiry to replace, and is returned if that fails.
            if (HasCachedId && CachedIdTimeUntilExpiry <= k_RefreshWindowBeforeExpiry)
            {
                return await FetchAndStoreWithNoRetries(playerId, playerData, deviceInfoFactory) ?? CachedIdIfPlayerUnchanged(playerId);
            }

            // The cached id is still valid.
            if (HasCachedId)
            {
                return m_CachedId;
            }

            return await FetchAndStoreWithNoRetries(playerId, playerData, deviceInfoFactory);
        }

        // The cached id, unless the current player or the cached id's player changed while a
        // replacement was requested.
        string? CachedIdIfPlayerUnchanged(string playerId) =>
            m_CoreRegistryHelper.PlayerId == playerId && m_CachedPlayerId == playerId ? m_CachedId : null;

        void Clear()
        {
            m_CachedId = null;
            m_CachedPlayerId = null;
            m_CachedIdCreatedAtUtc = default;
            m_CachedIdHasUnconfiguredFirebaseIds = false;
        }

        // Makes a single attempt to register a link-out session, returning null if it fails.
        async Task<string?> FetchAndStoreWithNoRetries(
            string playerId,
            IPlayerData playerData,
            Func<PaymentProviderService.Models.DeviceInfo?> deviceInfoFactory)
        {
            if (!m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                LogVerbose("Payment provider client unavailable; continuing without a link-out session id.");
                return null;
            }

            // The backend supplies no lifetime, so it is measured from when the request is sent.
            var requestedAtUtc = m_UtcNow();
            // Read before the identity is created: configure only goes one way, so ids collected
            // after a configured read are the configured ones.
            var firebaseDefaultAppConfigured = playerData.FirebaseDefaultAppConfigured;

            string? sessionId;
            try
            {
                sessionId = await m_PaymentProviderClientWrapper
                    .GetPaymentProviderService()
                    .RegisterLinkOutSession(
                        playerIdentity: await playerData.CreatePlayerIdentityAsync(impressionId: null),
                        deviceInfo: deviceInfoFactory()
                    );
            }
            catch (Exception e)
            {
                LogVerbose($"Could not register link-out session, continuing without one: {e.Message}");
                return null;
            }

            // A failed request leaves any previously stored id untouched.
            if (sessionId == null)
            {
                return null;
            }

            // The current player changed while the request was in flight, so the id is not theirs.
            if (m_CoreRegistryHelper.PlayerId != playerId)
            {
                return null;
            }

            m_CachedId = sessionId;
            m_CachedPlayerId = playerId;
            m_CachedIdCreatedAtUtc = requestedAtUtc;
            m_CachedIdHasUnconfiguredFirebaseIds = !firebaseDefaultAppConfigured;
            return sessionId;
        }

        void LogVerbose(string message,
            string className = nameof(LinkOutSessionIdProvider),
            [CallerMemberName] string callerName = "")
        {
            m_Logger.LogIAPCallVerbose(message, className, callerName);
        }
    }
}
