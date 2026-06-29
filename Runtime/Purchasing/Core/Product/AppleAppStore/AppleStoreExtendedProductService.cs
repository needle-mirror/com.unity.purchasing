#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AppleStoreExtendedProductService : ProductService, IAppleStoreExtendedProductService
    {
        readonly IFetchStorePromotionOrderUseCase m_FetchStorePromotionOrderUseCase;
        readonly IFetchStorePromotionVisibilityUseCase m_FetchStorePromotionVisibilityUseCase;
        readonly IGetIntroductoryPriceDictionaryUseCase m_GetIntroductoryPriceDictionaryUseCase;
        readonly IGetProductDetailsUseCase m_GetProductDetailsUseCase;
        readonly ISetStorePromotionOrderUseCase m_SetStorePromotionOrderUseCase;
        readonly ISetStorePromotionVisibilityUseCase m_SetStorePromotionVisibilityUseCase;

        [Preserve]
        internal AppleStoreExtendedProductService(
            IFetchStorePromotionOrderUseCase fetchStorePromotionOrderUseCase,
            IFetchStorePromotionVisibilityUseCase fetchStorePromotionVisibilityUseCase,
            IGetIntroductoryPriceDictionaryUseCase getIntroductoryPriceDictionaryUseCase,
            IGetProductDetailsUseCase getProductDetailsUseCase,
            ISetStorePromotionOrderUseCase setStorePromotionOrderUseCase,
            ISetStorePromotionVisibilityUseCase setStorePromotionVisibilityUseCase,
            IFetchProductsUseCase fetchProductsUseCase,
            IStoreWrapper storeWrapper)
            : base(fetchProductsUseCase, storeWrapper)
        {
            m_FetchStorePromotionOrderUseCase = fetchStorePromotionOrderUseCase;
            m_FetchStorePromotionVisibilityUseCase = fetchStorePromotionVisibilityUseCase;
            m_GetIntroductoryPriceDictionaryUseCase = getIntroductoryPriceDictionaryUseCase;
            m_GetProductDetailsUseCase = getProductDetailsUseCase;
            m_SetStorePromotionOrderUseCase = setStorePromotionOrderUseCase;
            m_SetStorePromotionVisibilityUseCase = setStorePromotionVisibilityUseCase;
        }

        public void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback)
        {
            m_FetchStorePromotionOrderUseCase.FetchStorePromotionOrder(successCallback, errorCallback);
        }

        public void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)
        {
            FetchStorePromotionVisibility(product.baseListing.id, successCallback, errorCallback);
        }

        public void FetchStorePromotionVisibility(string catalogListingId, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)
        {
            var product = GetProductById(catalogListingId);
            if (product == null || !product.catalogListings.TryGetValue(catalogListingId, out var listing))
            {
                errorCallback?.Invoke($"No catalog listing found for id: {catalogListingId}");
                return;
            }
            m_FetchStorePromotionVisibilityUseCase.FetchStorePromotionVisibility(listing.definition.storeSpecificId, successCallback, errorCallback);
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {
            return m_GetIntroductoryPriceDictionaryUseCase.GetIntroductoryPriceDictionary();
        }

        public Dictionary<string, string> GetProductDetails()
        {
            return m_GetProductDetailsUseCase.GetProductDetails();
        }

        public void SetStorePromotionOrder(List<Product> products)
        {
            var storeSpecificIds = new List<string>();
            foreach (var product in products)
            {
                var storeId = product?.baseListing?.definition.storeSpecificId;
                if (!string.IsNullOrEmpty(storeId))
                {
                    storeSpecificIds.Add(storeId);
                }
            }
            m_SetStorePromotionOrderUseCase.SetStorePromotionOrder(storeSpecificIds);
        }

        public void SetStorePromotionOrder(List<string> catalogListingIds)
        {
            var storeSpecificIds = new List<string>();
            foreach (var catalogListingId in catalogListingIds)
            {
                var product = GetProductByCatalogListingId(catalogListingId);
                if (product != null
                    && product.catalogListings.TryGetValue(catalogListingId, out var listing)
                    && !string.IsNullOrEmpty(listing.definition?.storeSpecificId))
                {
                    storeSpecificIds.Add(listing.definition.storeSpecificId);
                }
            }
            m_SetStorePromotionOrderUseCase.SetStorePromotionOrder(storeSpecificIds);
        }

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)
        {
            SetStorePromotionVisibility(product.baseListing.id, visible);
        }

        public void SetStorePromotionVisibility(string catalogListingId, AppleStorePromotionVisibility visible)
        {
            var product = GetProductById(catalogListingId);
            if (product == null || !product.catalogListings.TryGetValue(catalogListingId, out var listing))
            {
                return;
            }
            m_SetStorePromotionVisibilityUseCase.SetStorePromotionVisibility(listing.definition.storeSpecificId, visible);
        }
    }
}
