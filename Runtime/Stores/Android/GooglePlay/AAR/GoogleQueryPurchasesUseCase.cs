#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GoogleQueryPurchasesUseCase : IGoogleQueryPurchasesUseCase
    {
        readonly IBillingClient m_BillingClient;
        readonly IGooglePurchaseBuilder m_PurchaseBuilder;
        readonly IQueryProductDetailsService m_QueryProductDetailsService;
        readonly IGoogleCachedQueryProductDetailsService m_CachedQueryProductDetailsService;

        [Preserve]
        internal GoogleQueryPurchasesUseCase(
            IBillingClient billingClient,
            IGooglePurchaseBuilder purchaseBuilder,
            IQueryProductDetailsService queryProductDetailsService,
            IGoogleCachedQueryProductDetailsService cachedQueryProductDetailsService)
        {
            m_BillingClient = billingClient;
            m_PurchaseBuilder = purchaseBuilder;
            m_QueryProductDetailsService = queryProductDetailsService;
            m_CachedQueryProductDetailsService = cachedQueryProductDetailsService;
        }

        public async Task<List<IGooglePurchase>> QueryPurchases()
        {
            var purchaseResults = await Task.WhenAll(QueryPurchasesWithSkuType(GoogleProductTypeEnum.Sub()), QueryPurchasesWithSkuType(GoogleProductTypeEnum.InApp()));
            return purchaseResults.SelectMany(result => result).ToList();
        }

        async Task<IEnumerable<IGooglePurchase>> QueryPurchasesWithSkuType(string skuType)
        {
            var purchaseList = await GetRawPurchasesFromBillingClient(skuType);
            if (purchaseList == null)
            {
                return Enumerable.Empty<IGooglePurchase>();
            }

            try
            {
                await PreFetchMissingProductDetails(purchaseList);
                // Force materialization before the finally disposes the JNI clones —
                // BuildPurchases returns IEnumerable<IGooglePurchase>; even though the
                // current impl already ToList()s internally, calling .ToList() here makes
                // the lifetime guarantee obvious at the dispose site.
                return m_PurchaseBuilder.BuildPurchases(purchaseList).ToList();
            }
            finally
            {
                // Cloned global JNI references must be disposed explicitly. The C# GC is
                // unaware of JNI reference table pressure, so relying on finalization would
                // risk exhausting the JNI global reference table. BuildPurchases extracts
                // all needed data into POCOs (IGooglePurchase) and does not retain the
                // AndroidJavaObject references, so disposal here is safe.
                foreach (var purchase in purchaseList)
                {
                    purchase?.Dispose();
                }
            }
        }

        Task<List<AndroidJavaObject>?> GetRawPurchasesFromBillingClient(string skuType)
        {
            // RunContinuationsAsynchronously keeps the C# continuation off the
            // Google Billing / JNI callback thread that fires TrySetResult.
            var taskCompletion = new TaskCompletionSource<List<AndroidJavaObject>?>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_BillingClient.QueryPurchasesAsync(skuType,
                (billingResult, purchases) =>
                {
                    // Clone each JNI ref to a long-lived global ref BEFORE returning from the
                    // billing-client callback. Java releases the local refs as soon as the
                    // callback unwinds, and the subsequent QueryProductDetailsByRawSkus await
                    // gives Java time to do that — which would leave the AndroidJavaObject
                    // wrappers pointing at dead refs by the time BuildPurchases runs.
                    // QueryPurchasesWithSkuType's finally block disposes the clones.
                    //
                    // The outer try/catch guarantees the task always completes — without it,
                    // an exception from CloneReference (JNI table overflow, disposed input,
                    // etc.) would propagate out of the lambda, the TaskCompletionSource would
                    // never be set, and the awaiting caller would hang forever. The inner
                    // try/catch disposes any partial clones to avoid leaking JNI global refs.
                    try
                    {
                        List<AndroidJavaObject>? cloned = null;
                        if (IsResultOk(billingResult) && purchases != null)
                        {
                            cloned = new List<AndroidJavaObject>();
                            try
                            {
                                foreach (var purchase in purchases)
                                {
                                    if (purchase == null)
                                    {
                                        continue;
                                    }
                                    // GetRawObject() == IntPtr.Zero indicates either a test mock
                                    // (Moq-generated AndroidJavaObject with no JNI backing) or an
                                    // already-disposed reference. CloneReference would throw
                                    // "Cannot clone a disposed reference" in both cases; pass
                                    // through unchanged is the correct fallback for both —
                                    // production purchases from Google's billing client always
                                    // have a non-zero raw object and take the clone path.
                                    cloned.Add(purchase.GetRawObject() != IntPtr.Zero
                                        ? purchase.CloneReference()
                                        : purchase);
                                }
                            }
                            catch
                            {
                                foreach (var c in cloned)
                                {
                                    c?.Dispose();
                                }
                                throw;
                            }
                        }
                        taskCompletion.TrySetResult(cloned);
                    }
                    catch (Exception ex)
                    {
                        taskCompletion.TrySetException(ex);
                    }
                });
            return taskCompletion.Task;
        }

        async Task PreFetchMissingProductDetails(IEnumerable<AndroidJavaObject> purchases)
        {
            var missingSkus = purchases
                .SelectMany(GetSkusFromPurchase)
                .Distinct()
                .Where(sku => !m_CachedQueryProductDetailsService.ContainsSku(sku))
                .ToList();

            if (missingSkus.Count == 0)
            {
                return;
            }

            try
            {
                await m_QueryProductDetailsService.QueryProductDetailsByRawSkus(missingSkus);
            }
            catch (Exception)
            {
                // Best-effort. If the pre-fetch fails BuildPurchases falls back to its existing
                // skip-and-warn for any SKUs that are still uncached.
            }
        }

        static IEnumerable<string> GetSkusFromPurchase(AndroidJavaObject purchase)
        {
            try
            {
                using var getProductsObj = purchase.Call<AndroidJavaObject>("getProducts");
                return getProductsObj?.Enumerate<string>().ToList() ?? Enumerable.Empty<string>();
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
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
