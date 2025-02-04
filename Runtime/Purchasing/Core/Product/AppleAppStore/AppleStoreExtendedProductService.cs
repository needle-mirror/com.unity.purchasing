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
            m_FetchStorePromotionVisibilityUseCase.FetchStorePromotionVisibility(product, successCallback, errorCallback);
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
            m_SetStorePromotionOrderUseCase.SetStorePromotionOrder(products);
        }

        public void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)
        {
            m_SetStorePromotionVisibilityUseCase.SetStorePromotionVisibility(product, visible);
        }
    }
}
