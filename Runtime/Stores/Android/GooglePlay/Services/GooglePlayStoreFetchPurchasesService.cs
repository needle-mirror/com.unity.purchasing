#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreFetchPurchasesService : IGooglePlayStoreFetchPurchasesService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        readonly IGooglePurchaseConverter m_PurchaseConverter;
        IProductCache? m_ProductCache;
        IStorePurchaseFetchCallback? m_FetchCallback;
        IUtil m_Util;

        [Preserve]
        internal GooglePlayStoreFetchPurchasesService(IGooglePlayStoreService googlePlayStoreService,
            IGooglePlayStoreFinishTransactionService transactionService, IGooglePurchaseConverter purchaseConverter, IUtil util)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_PurchaseConverter = purchaseConverter;
            m_Util = util;
        }

        public void SetProductCache(IProductCache productCache)
        {
            m_ProductCache = productCache;
        }

        public void SetPurchaseFetchCallback(IStorePurchaseFetchCallback fetchCallback)
        {
            m_FetchCallback = fetchCallback;
        }

        public void FetchPurchases()
        {
            m_GooglePlayStoreService.FetchPurchases(OnPurchasesFetched, PurchaseRetrievalFailedForUnknownReasons);
        }

        public IGooglePurchase? GetGooglePurchase(string purchaseToken)
        {
            IGooglePurchase? purchase = null;
            m_GooglePlayStoreService.FetchPurchases(
                googlePurchases =>
                {
                    purchase = googlePurchases.FirstOrDefault(purchases => purchases.purchaseToken == purchaseToken);
                }, PurchaseRetrievalFailedForUnknownReasons);
            return purchase;
        }

        async void OnPurchasesFetched(List<IGooglePurchase>? purchases)
        {
            if (purchases == null)
            {
                PurchaseRetrievalFailedForUnknownReasons();
                return;
            }

            var orders = (await Task.WhenAll(purchases
                    .Select(purchase => m_PurchaseConverter.CreateOrderFromPurchase(purchase, m_ProductCache))))
                .ToList();

            m_FetchCallback?.OnAllPurchasesRetrieved(orders);

            var deferredPurchases = purchases.Where(PurchaseIsPending()).ToList();

            // OnAllPurchasesRetrieved is run on the main thread. In order to have UpdateDeferredProducts happen after
            // it, it needs to also be run on the main thread.
            m_Util.RunOnMainThread(() => UpdateDeferredProductsByPurchases(deferredPurchases));
        }

        void PurchaseRetrievalFailedForUnknownReasons(string? message = null)
        {
            m_FetchCallback?.OnPurchasesRetrievalFailed(
                new PurchasesFetchFailureDescription(PurchasesFetchFailureReason.Unknown, message ?? string.Empty));
        }

        static Func<IGooglePurchase, bool> PurchaseIsPending()
        {
            return purchase => purchase.IsPending();
        }

        void UpdateDeferredProductsByPurchases(List<IGooglePurchase> deferredPurchases)
        {
            foreach (var deferredPurchase in deferredPurchases)
            {
                UpdateDeferredProductsByPurchase(deferredPurchase);
            }
        }

        void UpdateDeferredProductsByPurchase(IGooglePurchase deferredPurchase)
        {
            foreach (var sku in deferredPurchase.skus)
            {
                UpdateDeferredProduct(deferredPurchase, sku);
            }
        }

        void UpdateDeferredProduct(IGooglePurchase deferredPurchase, string sku)
        {
            var product = m_ProductCache?.Find(sku);
            if (product != null)
            {
// Obsolete: Product.receipt, Product.transactionID
#pragma warning disable 618, 612
                product.receipt = deferredPurchase.receipt;
                product.transactionID = deferredPurchase.purchaseToken;
#pragma warning restore 618, 612
            }
        }
    }
}
