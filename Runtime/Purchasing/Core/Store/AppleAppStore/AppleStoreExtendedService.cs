#nullable enable

using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AppleStoreExtendedService : StoreService, IAppleStoreExtendedService
    {
        readonly ISetApplicationUsernameUseCase m_SetApplicationUsernameUseCase;
        readonly ICanMakePaymentsUseCase m_CanMakePaymentsUseCase;
        readonly IClearAppleTransactionLogsUseCase m_ClearAppleTransactionLogsUseCase;

        [Preserve]
        internal AppleStoreExtendedService(
            ICanMakePaymentsUseCase canMakePaymentsUseCase,
            ISetApplicationUsernameUseCase setApplicationUsernameUseCase,
            IClearAppleTransactionLogsUseCase clearAppleTransactionLogsUseCase,
            IStoreConnectUseCase connectUseCase,
            IRetryPolicy? defaultConnectionRetryPolicy)
            : base(connectUseCase, defaultConnectionRetryPolicy)
        {
            m_CanMakePaymentsUseCase = canMakePaymentsUseCase;
            m_SetApplicationUsernameUseCase = setApplicationUsernameUseCase;
            m_ClearAppleTransactionLogsUseCase = clearAppleTransactionLogsUseCase;
        }

        public bool canMakePayments => m_CanMakePaymentsUseCase.CanMakePayments();

        public void SetApplicationUsername(string applicationUsername)
        {
            m_SetApplicationUsernameUseCase.SetApplicationUsername(applicationUsername);
        }

        public void ClearTransactionLog()
        {
#if DEBUG
            m_ClearAppleTransactionLogsUseCase.ClearTransactionLog();
#endif
        }
    }
}
