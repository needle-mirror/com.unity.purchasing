#nullable enable

using System;
using System.Collections.Generic;
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
            return JSONSerializer.DeserializeProductDetails(m_FetchProductsService.LastRequestProductsJson);
        }
    }
}
