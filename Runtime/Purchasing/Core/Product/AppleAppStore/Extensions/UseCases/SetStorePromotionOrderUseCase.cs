#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class SetStorePromotionOrderUseCase : ISetStorePromotionOrderUseCase
    {
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal SetStorePromotionOrderUseCase(INativeAppleStore nativeStore)
        {
            m_NativeAppleStore = nativeStore;
        }

        public void SetStorePromotionOrder(List<Product> products)
        {
            // Encode product list as a json doc containing an array of store-specific ids:
            // { "products": [ "ssid1", "ssid2" ] }
            var productIds = new List<string>();
            foreach (var p in products)
            {
                if (p != null && !string.IsNullOrEmpty(p.definition.storeSpecificId))
                {
                    productIds.Add(p.definition.storeSpecificId);
                }
            }

            m_NativeAppleStore.SetStorePromotionOrder(MiniJson.JsonEncode(productIds));
        }
    }
}
