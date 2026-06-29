#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Stores;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreFetchProductsService : IGooglePlayStoreFetchProductsService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        readonly IStoreLocationContext m_StoreLocationContext;
        IStoreProductsCallback? m_ProductsCallback;

        [Preserve]
        internal GooglePlayStoreFetchProductsService(IGooglePlayStoreService googlePlayStoreService,
            IStoreLocationContext storeLocationContext)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
            m_StoreLocationContext = storeLocationContext;
        }

        public void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_GooglePlayStoreService.FetchProducts(products, OnProductsFetched, OnFetchProductsFailed);
        }

        void OnProductsFetched(List<ProductDescription> retrievedProducts)
        {
            ExtractCurrencyFromProducts(retrievedProducts);
            m_ProductsCallback?.OnProductsFetched(retrievedProducts);
        }

        void ExtractCurrencyFromProducts(List<ProductDescription> products)
        {
            foreach (var product in products)
            {
                var currency = product.metadata?.isoCurrencyCode;
                if (!string.IsNullOrEmpty(currency))
                {
                    m_StoreLocationContext.CurrencyCode = currency;
                    break;
                }
            }
        }

        void OnFetchProductsFailed(GoogleFetchProductException exception)
        {
            m_ProductsCallback?.OnProductsFetchFailed(exception.FailureDescription);
        }

        [Obsolete]
        static List<ProductDescription> MakePurchasesIntoProducts(List<ProductDescription> retrievedProducts, IEnumerable<Product> purchaseProducts)
        {
            var updatedProducts = new List<ProductDescription>(retrievedProducts);
            if (purchaseProducts != null)
            {
                foreach (var purchaseProduct in purchaseProducts)
                {
                    var purchaseStoreId = purchaseProduct.baseListing?.definition.storeSpecificId;
                    var retrievedProductIndex = updatedProducts.FindLastIndex(product => product.storeSpecificId == purchaseStoreId);
                    if (retrievedProductIndex != -1)
                    {
                        var retrievedProduct = updatedProducts[retrievedProductIndex];
                        updatedProducts[retrievedProductIndex] = new ProductDescription(retrievedProduct.storeSpecificId, retrievedProduct.metadata, purchaseProduct.receipt, purchaseProduct.transactionID, retrievedProduct.type);
                    }
                }
            }

            return updatedProducts;
        }

        public void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            m_ProductsCallback = productsCallback;
        }
    }
}
