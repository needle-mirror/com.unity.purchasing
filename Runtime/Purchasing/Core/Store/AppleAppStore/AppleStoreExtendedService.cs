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
            IStoreConnectUseCase connectUseCase,
            IStoreWrapper storeWrapper)
            : base(connectUseCase, storeWrapper)
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

        [Obsolete("Use ClearTransactionLog(bool debugOnly) instead. This method behaves like ClearTransactionLog(true).", false)]
        public void ClearTransactionLog()
        {
            ClearTransactionLog(true);
        }

        public void ClearTransactionLog(bool debugOnly)
        {
#if !(DEBUG || IAP_APPLE_CLEAR_TRANSACTION_LOG)
            if (debugOnly)
            {
                return;
            }
#endif
            m_ClearAppleTransactionLogsUseCase.ClearTransactionLog();
        }

        public void FetchStorefront(Action<AppleStorefront> successCallback, Action<string> errorCallback)
        {
            using (ImpressionScope.Shared.EnterNone())
            {
                m_FetchStorefrontUseCase.FetchStorefront(successCallback, errorCallback);
            }
        }
    }
}
