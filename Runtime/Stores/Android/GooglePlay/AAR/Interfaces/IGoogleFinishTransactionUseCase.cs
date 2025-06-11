using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleFinishTransactionUseCase
    {
        Task FinishTransaction(ProductDefinition product, string purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished);
    }
}
