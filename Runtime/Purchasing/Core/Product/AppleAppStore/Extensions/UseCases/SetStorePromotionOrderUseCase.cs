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

        public void SetStorePromotionOrder(List<string> storeSpecificIds)
        {
            // Encode the store-specific ids as a json array: ["ssid1", "ssid2"]
            m_NativeAppleStore.SetStorePromotionOrder(MiniJson.JsonEncode(storeSpecificIds));
        }
    }
}
