#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Interfaces
{
    public interface IGoogleQueryPurchasesUseCase
    {
        Task<List<IGooglePurchase>> QueryPurchases();
        Task<IGooglePurchase?> GetPurchaseByToken(string? purchaseToken);
    }
}