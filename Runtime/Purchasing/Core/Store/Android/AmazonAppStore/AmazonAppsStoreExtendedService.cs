#nullable enable
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing.Services
{
    class AmazonAppsStoreExtendedService : StoreService, IAmazonAppsStoreExtendedService
    {
        IAmazonAppsGetAmazonUserIdUseCase m_GetAmazonUserIdUseCase;

        internal AmazonAppsStoreExtendedService(
            IAmazonAppsGetAmazonUserIdUseCase getAmazonUserIdUseCase,
            IStoreConnectUseCase connectUseCase,
            IRetryPolicy? defaultConnectionRetryPolicy)
            : base(connectUseCase, defaultConnectionRetryPolicy)
        {
            m_GetAmazonUserIdUseCase = getAmazonUserIdUseCase;
        }

        /// <summary>
        /// Gets the current Amazon user ID (for other Amazon services).
        /// </summary>
        public string amazonUserId => GetAmazonUserId();

        /// <summary>
        /// Gets the current Amazon user ID (for other Amazon services).
        /// </summary>
        public string GetAmazonUserId()
        {
            return m_GetAmazonUserIdUseCase.GetAmazonUserId();
        }
    }
}
