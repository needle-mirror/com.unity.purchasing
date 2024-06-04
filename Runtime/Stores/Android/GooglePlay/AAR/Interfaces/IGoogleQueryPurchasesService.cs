#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleQueryPurchasesService
    {
        Task<List<IGooglePurchase>> QueryPurchases();
        IGooglePurchase? GetPurchaseByToken(string token, string skuType);
    }
}
