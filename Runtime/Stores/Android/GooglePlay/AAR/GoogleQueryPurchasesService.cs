using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GoogleQueryPurchasesService : IGoogleQueryPurchasesService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGooglePurchaseBuilder m_PurchaseBuilder;

        internal GoogleQueryPurchasesService(IGoogleBillingClient billingClient, IGooglePurchaseBuilder purchaseBuilder)
        {
            m_BillingClient = billingClient;
            m_PurchaseBuilder = purchaseBuilder;
        }

        public async Task<List<IGooglePurchase>> QueryPurchases()
        {
            var purchaseResults = await Task.WhenAll(QueryPurchasesWithSkuType(GoogleSkuTypeEnum.Sub()), QueryPurchasesWithSkuType(GoogleSkuTypeEnum.InApp()));
            return purchaseResults.SelectMany(result => result).ToList();
        }

        Task<IEnumerable<IGooglePurchase>> QueryPurchasesWithSkuType(string skuType)
        {
            var taskCompletion = new TaskCompletionSource<IEnumerable<IGooglePurchase>>();
            m_BillingClient.QueryPurchasesAsync(skuType,
                (billingResult, purchases) =>
                {
                    var result = IsResultOk(billingResult) ? m_PurchaseBuilder.BuildPurchases(purchases) : Enumerable.Empty<IGooglePurchase>();
                    taskCompletion.SetResult(result);
                });

            return taskCompletion.Task;
        }

        static bool IsResultOk(IGoogleBillingResult result)
        {
            return result.responseCode == GoogleBillingResponseCode.Ok;
        }
    }
}
