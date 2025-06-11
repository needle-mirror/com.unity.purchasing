#nullable enable
using System;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreExtendedService : StoreService, IGooglePlayStoreExtendedService
    {
        Action? m_InitializationConnectionListener;
        readonly IGooglePlayStoreSetObfuscatedIdUseCase m_GooglePlayStoreSetObfuscatedIdUseCase;
        readonly IGooglePlayStoreConnectionUseCase m_GooglePlayStoreConnectionUseCase;

        [Preserve]
        internal GooglePlayStoreExtendedService(
            IGooglePlayStoreSetObfuscatedIdUseCase googlePlayStoreSetObfuscatedIdUseCase,
            IGooglePlayStoreConnectionUseCase googlePlayStoreConnectionUseCase,
            IStoreConnectUseCase connectUseCase)
            : base(connectUseCase)
        {
            m_GooglePlayStoreSetObfuscatedIdUseCase = googlePlayStoreSetObfuscatedIdUseCase;
            m_GooglePlayStoreConnectionUseCase = googlePlayStoreConnectionUseCase;
        }

        /// <summary>
        /// Internal API, do not use.
        /// </summary>
        public void SetInitializeConnectionFailureListener(Action? listener)
        {
            m_InitializationConnectionListener = listener;
        }

        /// <summary>
        /// Internal API, do not use.
        /// </summary>
        public void NotifyInitializationConnectionFailed()
        {
            m_InitializationConnectionListener?.Invoke();
        }

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase.
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="accountId">The obfuscated account id</param>
        public void SetObfuscatedAccountId(string accountId)
        {
            m_GooglePlayStoreSetObfuscatedIdUseCase.SetObfuscatedAccountId(accountId);
        }

        /// <summary>
        /// Optional obfuscation string to detect irregular activities when making a purchase
        /// For more information please visit <a href="https://developer.android.com/google/play/billing/security">https://developer.android.com/google/play/billing/security</a>
        /// </summary>
        /// <param name="profileId">The obfuscated profile id</param>
        public void SetObfuscatedProfileId(string profileId)
        {
            m_GooglePlayStoreSetObfuscatedIdUseCase.SetObfuscatedProfileId(profileId);
        }
        public void EndConnection()
        {
            m_GooglePlayStoreConnectionUseCase.EndConnection();
        }
    }
}
