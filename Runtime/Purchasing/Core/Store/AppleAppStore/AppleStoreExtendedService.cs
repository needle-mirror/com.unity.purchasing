#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AppleStoreExtendedService : StoreService, IAppleStoreExtendedService
    {
        readonly ICanMakePaymentsUseCase m_CanMakePaymentsUseCase;
        readonly IClearAppleTransactionLogsUseCase m_ClearAppleTransactionLogsUseCase;
        readonly ISetAppAccountTokenUseCase m_SetAppAccountTokenUseCase;
        readonly IFetchStorefrontUseCase m_FetchStorefrontUseCase;

        [Preserve]
        internal AppleStoreExtendedService(
            ICanMakePaymentsUseCase canMakePaymentsUseCase,
            IClearAppleTransactionLogsUseCase clearAppleTransactionLogsUseCase,
            ISetAppAccountTokenUseCase setAppAccountTokenUseCase,
            IFetchStorefrontUseCase fetchStorefrontUseCase,
            IStoreConnectUseCase connectUseCase)
            : base(connectUseCase)
        {
            m_CanMakePaymentsUseCase = canMakePaymentsUseCase;
            m_ClearAppleTransactionLogsUseCase = clearAppleTransactionLogsUseCase;
            m_SetAppAccountTokenUseCase = setAppAccountTokenUseCase;
            m_FetchStorefrontUseCase = fetchStorefrontUseCase;
        }

        public bool canMakePayments => m_CanMakePaymentsUseCase.CanMakePayments();

        public void SetAppAccountToken(Guid appAccountToken)
        {
            m_SetAppAccountTokenUseCase.SetAppAccountToken(appAccountToken);
        }

        public void ClearTransactionLog()
        {
#if DEBUG
            m_ClearAppleTransactionLogsUseCase.ClearTransactionLog();
#endif
        }

        public void FetchStorefront(Action<AppleStorefront> successCallback, Action<string> errorCallback)
        {
            m_FetchStorefrontUseCase.FetchStorefront(successCallback, errorCallback);
        }
    }
}
