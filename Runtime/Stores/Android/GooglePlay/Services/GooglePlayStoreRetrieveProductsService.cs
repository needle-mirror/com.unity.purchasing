#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayStoreRetrieveProductsService : IGooglePlayStoreRetrieveProductsService
    {
        readonly IGooglePlayStoreService m_GooglePlayStoreService;
        IStoreProductsCallback? m_ProductsCallback;
        IProductCache? m_ProductCache;

        [Preserve]
        internal GooglePlayStoreRetrieveProductsService(IGooglePlayStoreService googlePlayStoreService)
        {
            m_GooglePlayStoreService = googlePlayStoreService;
        }

        public void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            m_ProductCache?.AddStoreSpecificIds(products);
            m_GooglePlayStoreService.RetrieveProducts(products, OnProductsRetrieved, OnRetrieveProductsFailed);
        }

        void OnProductsRetrieved(List<ProductDescription> retrievedProducts)
        {
            m_ProductCache?.Add(retrievedProducts);
            m_ProductsCallback?.OnProductsRetrieved(retrievedProducts);
        }

        void OnRetrieveProductsFailed(GoogleRetrieveProductException exception)
        {
            m_ProductsCallback?.OnProductsRetrieveFailed(exception.FailureDescription);
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

        public void SetProductCache(IProductCache? productCache)
        {
            m_ProductCache = productCache;
        }

        public void SetProductsCallback(IStoreProductsCallback productsCallback)
        {
            m_ProductsCallback = productsCallback;
        }
    }
}
