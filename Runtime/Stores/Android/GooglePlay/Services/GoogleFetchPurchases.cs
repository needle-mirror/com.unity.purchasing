using System;
using System.Collections.Generic;
using System.Linq;
using Uniject;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class GoogleFetchPurchases : IGoogleFetchPurchases
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreCallback m_StoreCallback;
        IUtil m_Util;

        internal GoogleFetchPurchases(IGooglePlayStoreService googlePlayStoreService, IUtil util)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_Util = util;
        }

        public void SetStoreCallback(IStoreCallback storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public void FetchPurchases()
        {
            m_GooglePlayStoreService.FetchPurchases(OnFetchedPurchase);
        }

        public void FetchPurchases(Action<List<Product>> onQueryPurchaseSucceed)
        {
            m_GooglePlayStoreService.FetchPurchases(
                googlePurchases =>
                {
                    onQueryPurchaseSucceed(FillProductsWithPurchases(googlePurchases));
                });
        }

        List<Product> FillProductsWithPurchases(IEnumerable<IGooglePurchase> purchases)
        {
            return purchases.SelectMany(BuildProductsFromPurchase).ToList();
        }

        IEnumerable<Product> BuildProductsFromPurchase(IGooglePurchase purchase)
        {
            var products = purchase?.skus?.Select(sku => m_StoreCallback?.FindProductById(sku)).NonNull();
            return products?.Select(product => CompleteProductInfoWithPurchase(product, purchase));
        }

        static Product CompleteProductInfoWithPurchase(Product product, IGooglePurchase purchase)
        {
            return new Product(product.definition, product.metadata, purchase.receipt)
            {
                transactionID = purchase.purchaseToken,
            };
        }

        void OnFetchedPurchase(List<IGooglePurchase> purchases)
        {
            var purchasedPurchases = purchases.Where(PurchaseIsPurchased()).ToList();
            var purchasedProducts = FillProductsWithPurchases(purchasedPurchases);
            if (!purchasedProducts.Any())
            {
                return;
            }

            m_StoreCallback?.OnAllPurchasesRetrieved(purchasedProducts);

            var deferredPurchases = purchases.Where(PurchaseIsPending()).ToList();

            // OnAllPurchasesRetrieved is run on the main thread. In order to have UpdateDeferredProducts happen after
            // it, it needs to also be run on the main thread.
            m_Util.RunOnMainThread(() => UpdateDeferredProductsByPurchases(deferredPurchases));
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
            var product = m_StoreCallback?.FindProductById(sku);
            if (product != null)
            {
                product.receipt = deferredPurchase.receipt;
                product.transactionID = deferredPurchase.purchaseToken;
            }
        }
    }
}
