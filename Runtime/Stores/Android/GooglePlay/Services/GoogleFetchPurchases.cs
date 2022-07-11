using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GoogleFetchPurchases : IGoogleFetchPurchases
    {
        IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreCallback m_StoreCallback;

        internal GoogleFetchPurchases(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
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
            var purchasedProducts = FillProductsWithPurchases(purchases);
            if (purchasedProducts.Any())
            {
                m_StoreCallback?.OnAllPurchasesRetrieved(purchasedProducts);
            }
        }
    }
}
