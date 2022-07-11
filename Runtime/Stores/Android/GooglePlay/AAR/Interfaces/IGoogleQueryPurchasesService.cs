using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleQueryPurchasesService
    {
        Task<List<IGooglePurchase>> QueryPurchases();
    }
}
