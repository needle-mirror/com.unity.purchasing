#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AppleStoreExtendedService : StoreService, IAppleStoreExtendedService
    {
        readonly ISetApplicationUsernameUseCase m_SetApplicationUsernameUseCase;
        readonly ICanMakePaymentsUseCase m_CanMakePaymentsUseCase;
        readonly IClearAppleTransactionLogsUseCase m_ClearAppleTransactionLogsUseCase;
        readonly ISetAppAccountTokenUseCase m_SetAppAccountTokenUseCase;

        [Preserve]
        internal AppleStoreExtendedService(
            ICanMakePaymentsUseCase canMakePaymentsUseCase,
            ISetApplicationUsernameUseCase setApplicationUsernameUseCase,
            IClearAppleTransactionLogsUseCase clearAppleTransactionLogsUseCase,
            ISetAppAccountTokenUseCase setAppAccountTokenUseCase,
            IStoreConnectUseCase connectUseCase,
            IRetryPolicy? defaultConnectionRetryPolicy)
            : base(connectUseCase, defaultConnectionRetryPolicy)
        {
            m_CanMakePaymentsUseCase = canMakePaymentsUseCase;
            m_SetApplicationUsernameUseCase = setApplicationUsernameUseCase;
            m_ClearAppleTransactionLogsUseCase = clearAppleTransactionLogsUseCase;
            m_SetAppAccountTokenUseCase = setAppAccountTokenUseCase;
        }

        public bool canMakePayments => m_CanMakePaymentsUseCase.CanMakePayments();

        public void SetApplicationUsername(string applicationUsername)
        {
            m_SetApplicationUsernameUseCase.SetApplicationUsername(applicationUsername);
        }

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
    }
}
