#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Security;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreRetrieveProductsService : IGooglePlayStoreRetrieveProductsService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        readonly IGoogleFetchPurchases m_GoogleFetchPurchases;
        IStoreCallback? m_StoreCallback;
        readonly IGooglePlayConfigurationInternal m_GooglePlayConfigurationInternal;
        readonly IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;
        bool m_HasInitiallyRetrievedProducts;
        bool m_RetrieveProductsFailed;

        internal GooglePlayStoreRetrieveProductsService(IGooglePlayStoreService googlePlayStoreService,
            IGoogleFetchPurchases googleFetchPurchases,
            IGooglePlayConfigurationInternal googlePlayConfigurationInternal,
            IGooglePlayStoreExtensions googlePlayStoreExtensions)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_GoogleFetchPurchases = googleFetchPurchases;
            m_GooglePlayConfigurationInternal = googlePlayConfigurationInternal;
            m_GooglePlayStoreExtensions = googlePlayStoreExtensions;

            m_HasInitiallyRetrievedProducts = false;
            m_RetrieveProductsFailed = false;
        }

        public void SetStoreCallback(IStoreCallback? storeCallback)
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
            m_RetrieveProductsFailed = false;

            m_StoreCallback?.OnProductsRetrieved(retrievedProducts);
        }

        void OnRetrieveProductsFailed(GoogleRetrieveProductsFailureReason reason, GoogleBillingResponseCode responseCode)
        {
            if (reason == GoogleRetrieveProductsFailureReason.BillingServiceUnavailable &&
                !m_HasInitiallyRetrievedProducts && !m_RetrieveProductsFailed)
            {
                m_RetrieveProductsFailed = true;
                m_GooglePlayConfigurationInternal.NotifyInitializationConnectionFailed();
                m_StoreCallback?.OnSetupFailed(InitializationFailureReason.PurchasingUnavailable, $"GoogleBillingResponseCode: {responseCode.ToString()}");
            }
        }

        public void ResumeConnection()
        {
            m_GooglePlayStoreService.ResumeConnection();
        }

        List<ProductDescription> MakePurchasesIntoProducts(List<ProductDescription> retrievedProducts, IEnumerable<Product> purchaseProducts)
        {
            var updatedProducts = new List<ProductDescription>(retrievedProducts);
            if (purchaseProducts != null)
            {
                foreach (var purchaseProduct in purchaseProducts)
                {
                    if (m_GooglePlayConfigurationInternal.DoesRetrievePurchasesExcludeDeferred() &&
                        IsPurchasedProductDeferred(purchaseProduct))
                    {
                        continue;
                    }

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

        bool IsPurchasedProductDeferred(Product product)
        {
            var tmpProduct = CreateNewProductUnifiedReceipt(product);
            return m_GooglePlayStoreExtensions.IsPurchasedProductDeferred(tmpProduct);
        }

        static Product CreateNewProductUnifiedReceipt(Product product)
        {
            var unifiedReceipt = UnifiedReceiptFormatter.FormatUnifiedReceipt(product.receipt,
                product.transactionID, GooglePlay.Name);
            return new Product(product.definition, product.metadata, unifiedReceipt);
        }

        public bool HasInitiallyRetrievedProducts()
        {
            return m_HasInitiallyRetrievedProducts;
        }
    }
}
