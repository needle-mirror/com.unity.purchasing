#nullable enable

using System;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayRestoreTransactionUseCase : IRestoreTransactionsUseCase
    {
        readonly IFetchPurchasesUseCase m_FetchPurchasesUseCase;

        [Preserve]
        public GooglePlayRestoreTransactionUseCase(IFetchPurchasesUseCase fetchPurchasesUseCase)
        {
            m_FetchPurchasesUseCase = fetchPurchasesUseCase;
        }

        public void RestoreTransactions(Action<bool, string?>? callback)
        {
            Action<Orders> successCallback = _ => { callback?.Invoke(true, null); };
            Action<PurchasesFetchFailureDescription> failureCallback = (desc) => { callback?.Invoke(false, desc.message); };

            m_FetchPurchasesUseCase.FetchPurchases(successCallback, failureCallback);
        }
    }
}
