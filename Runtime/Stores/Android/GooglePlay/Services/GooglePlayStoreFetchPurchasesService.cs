#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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

        public void FetchPurchases(Action<List<Product>> onQueryPurchaseSucceed)
        {
            m_GooglePlayStoreService.FetchPurchases(
                googlePurchases =>
                {
                    onQueryPurchaseSucceed(FillProductsWithPurchases(googlePurchases));
                }, PurchaseRetrievalFailedForUnknownReasons);
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

        List<Product> FillProductsWithPurchases(IEnumerable<IGooglePurchase> purchases)
        {
            return purchases.SelectMany(BuildProductsFromPurchase).ToList();
        }

        IEnumerable<Product> BuildProductsFromPurchase(IGooglePurchase purchase)
        {
            var products = purchase.skus.Select(sku => m_ProductCache?.Find(sku)).NonNull();
            return products.Select(product => CompleteProductInfoWithPurchase(product!, purchase));
        }

        static Product CompleteProductInfoWithPurchase(Product product, IGooglePurchase purchase)
        {
            return new Product(product.definition, product.metadata, purchase.receipt)
            {
                transactionID = purchase.purchaseToken,
            };
        }

        void OnPurchasesFetched(List<IGooglePurchase>? purchases)
        {
            if (purchases == null)
            {
                PurchaseRetrievalFailedForUnknownReasons();
                return;
            }

            var orders = purchases
                .Select(purchase => m_PurchaseConverter.CreateOrderFromPurchase(purchase, m_ProductCache))
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

        static Func<IGooglePurchase, bool> PurchaseIsPurchased()
        {
            return purchase => purchase.IsPurchased();
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
                product.receipt = deferredPurchase.receipt;
                product.transactionID = deferredPurchase.purchaseToken;
            }
        }
    }
}
