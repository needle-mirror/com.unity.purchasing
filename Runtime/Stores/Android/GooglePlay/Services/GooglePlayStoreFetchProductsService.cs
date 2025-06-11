#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreFetchProductsService : IGooglePlayStoreFetchProductsService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreProductsCallback? m_ProductsCallback;

        [Preserve]
        internal GooglePlayStoreFetchProductsService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_GooglePlayStoreService.FetchProducts(products, OnProductsFetched, OnFetchProductsFailed);
        }

        void OnProductsFetched(List<ProductDescription> retrievedProducts)
        {
            m_ProductsCallback?.OnProductsFetched(retrievedProducts);
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

        public void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            m_ProductsCallback = productsCallback;
        }
    }
}
