#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GoogleQueryPurchasesUseCase : IGoogleQueryPurchasesUseCase
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGooglePurchaseBuilder m_PurchaseBuilder;

        [Preserve]
        internal GoogleQueryPurchasesUseCase(IGoogleBillingClient billingClient, IGooglePurchaseBuilder purchaseBuilder)
        {
            m_BillingClient = billingClient;
            m_PurchaseBuilder = purchaseBuilder;
        }

        public async Task<List<IGooglePurchase>> QueryPurchases()
        {
            var purchaseResults = await Task.WhenAll(QueryPurchasesWithSkuType(GoogleProductTypeEnum.Sub()), QueryPurchasesWithSkuType(GoogleProductTypeEnum.InApp()));
            return purchaseResults.SelectMany(result => result).ToList();
        }

        Task<IEnumerable<IGooglePurchase>> QueryPurchasesWithSkuType(string skuType)
        {
            var taskCompletion = new TaskCompletionSource<IEnumerable<IGooglePurchase>>();
            m_BillingClient.QueryPurchasesAsync(skuType,
                (billingResult, purchases) =>
                {
                    var result = IsResultOk(billingResult) ? m_PurchaseBuilder.BuildPurchases(purchases) : Enumerable.Empty<IGooglePurchase>();
                    taskCompletion.TrySetResult(result);
                });

            return taskCompletion.Task;
        }

        public async Task<IGooglePurchase?> GetPurchaseByToken(string? purchaseToken)
        {
            var purchases = await QueryPurchases();
            var purchase = purchases.NonNull().FirstOrDefault(purchase => purchase.purchaseToken == purchaseToken);

            return purchase;
        }

        static bool IsResultOk(IGoogleBillingResult result)
        {
            return result.responseCode == GoogleBillingResponseCode.Ok;
        }
    }
}
