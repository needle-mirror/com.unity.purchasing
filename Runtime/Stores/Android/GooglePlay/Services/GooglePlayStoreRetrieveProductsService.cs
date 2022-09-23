using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreRetrieveProductsService : IGooglePlayStoreRetrieveProductsService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        readonly IGoogleFetchPurchases m_GoogleFetchPurchases;
        IStoreCallback m_StoreCallback;
        readonly IGooglePlayConfigurationInternal m_GooglePlayConfigurationInternal;
        bool m_HasInitiallyRetrievedProducts;

        internal GooglePlayStoreRetrieveProductsService(IGooglePlayStoreService googlePlayStoreService, IGoogleFetchPurchases googleFetchPurchases, IGooglePlayConfigurationInternal googlePlayConfigurationInternal)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_GoogleFetchPurchases = googleFetchPurchases;
            m_GooglePlayConfigurationInternal = googlePlayConfigurationInternal;

            m_HasInitiallyRetrievedProducts = false;
        }

        public void SetStoreCallback(IStoreCallback storeCallback)
        {
            m_StoreCallback = storeCallback;
        }

        public void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products, bool wantPurchases = true)
        {
            if (wantPurchases)
            {
                m_GooglePlayStoreService.RetrieveProducts(products, OnProductsRetrievedWithPurchaseFetch, OnRetrieveProductsFailed);
            }
            else
            {
                m_GooglePlayStoreService.RetrieveProducts(products, OnProductsRetrieved, OnRetrieveProductsFailed);
            }
        }

        void OnProductsRetrievedWithPurchaseFetch(List<ProductDescription> retrievedProducts)
        {
            m_HasInitiallyRetrievedProducts = true;

            m_GoogleFetchPurchases.FetchPurchases(purchaseProducts =>
            {
                var mergedProducts = MakePurchasesIntoProducts(retrievedProducts, purchaseProducts);
                m_StoreCallback?.OnProductsRetrieved(mergedProducts);
            });
        }

        void OnProductsRetrieved(List<ProductDescription> retrievedProducts)
        {
            m_HasInitiallyRetrievedProducts = true;

            m_StoreCallback?.OnProductsRetrieved(retrievedProducts);
        }

        void OnRetrieveProductsFailed(GoogleRetrieveProductsFailureReason reason)
        {
            if (reason == GoogleRetrieveProductsFailureReason.BillingServiceUnavailable && !m_HasInitiallyRetrievedProducts)
            {
                m_GooglePlayConfigurationInternal.NotifyInitializationConnectionFailed();
                m_StoreCallback.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable);
            }
        }

        public void ResumeConnection()
        {
            m_GooglePlayStoreService.ResumeConnection();
        }

        static List<ProductDescription> MakePurchasesIntoProducts(List<ProductDescription> retrievedProducts, IEnumerable<Product> purchaseProducts)
        {
            var updatedProducts = new List<ProductDescription>(retrievedProducts);
            if (purchaseProducts != null)
            {
                foreach (var purchaseProduct in purchaseProducts)
                {
                    var retrievedProductIndex = updatedProducts.FindLastIndex(product => product.storeSpecificId == purchaseProduct.definition.storeSpecificId);
                    if (retrievedProductIndex != -1)
                    {
                        var retrievedProduct = updatedProducts[retrievedProductIndex];
                        updatedProducts[retrievedProductIndex] = new ProductDescription(retrievedProduct.storeSpecificId, retrievedProduct.metadata, purchaseProduct.receipt, purchaseProduct.transactionID, retrievedProduct.type);
                    }
                }
            }
            return updatedProducts;
        }

        public bool HasInitiallyRetrievedProducts()
        {
            return m_HasInitiallyRetrievedProducts;
        }
    }
}
