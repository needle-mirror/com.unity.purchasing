#nullable enable

using System;
using System.Collections.Generic;
using Purchasing.Utilities;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class GetProductDetailsUseCase : IGetProductDetailsUseCase
    {
        readonly IAppleFetchProductsService m_FetchProductsService;

        [Preserve]
        internal GetProductDetailsUseCase(IAppleFetchProductsService fetchProductsService)
        {
            m_FetchProductsService = fetchProductsService;
        }

        public Dictionary<string, string> GetProductDetails()
        {
            var json = m_FetchProductsService.LastRequestProductsJson;
            return StoreKitSelector.UseStoreKit1()
                ? JSONSerializer.DeserializeProductDetailsSK1(json)
                : JSONSerializer.DeserializeProductDetailsSK2(json);
        }
    }
}
