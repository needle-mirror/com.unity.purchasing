#nullable enable

using System;
using System.Collections.Generic;
using Purchasing.Utilities;
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
            var json = m_FetchProductsService.FetchedProductsJson;
            return StoreKitSelector.UseStoreKit1()
                ? JSONSerializer.DeserializeSubscriptionDescriptions(json)
                : JSONSerializer.DeserializeSubscriptionDescriptionsSK2(json);
        }
    }
}
