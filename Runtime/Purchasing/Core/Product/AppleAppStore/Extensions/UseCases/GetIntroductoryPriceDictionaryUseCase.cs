#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class GetIntroductoryPriceDictionaryUseCase : IGetIntroductoryPriceDictionaryUseCase
    {
        readonly IAppleFetchProductsService m_FetchProductsService;

        [Preserve]
        internal GetIntroductoryPriceDictionaryUseCase(IAppleFetchProductsService fetchProductsService)
        {
            m_FetchProductsService = fetchProductsService;
        }

        public Dictionary<string, string> GetIntroductoryPriceDictionary()
        {
            return JSONSerializer.DeserializeSubscriptionDescriptions(m_FetchProductsService
                .LastRequestProductsJson);
        }
    }
}
