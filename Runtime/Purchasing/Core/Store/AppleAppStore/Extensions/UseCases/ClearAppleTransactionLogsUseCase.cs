using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    class ClearAppleTransactionLogsUseCase : IClearAppleTransactionLogsUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal ClearAppleTransactionLogsUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }

        public void ClearTransactionLog()
        {
            m_AppleStoreCallbacks.ClearTransactionLog();
        }
    }
}
