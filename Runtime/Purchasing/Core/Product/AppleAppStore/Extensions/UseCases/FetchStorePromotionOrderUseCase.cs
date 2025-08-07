#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class FetchStorePromotionOrderUseCase : IFetchStorePromotionOrderUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal FetchStorePromotionOrderUseCase(IAppleStoreCallbacks appleStoreCallbacks, INativeAppleStore nativeStore)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
            m_NativeAppleStore = nativeStore;
        }

        public void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback)
        {
            m_AppleStoreCallbacks.SetFetchStorePromotionOrderCallbacks(successCallback, errorCallback);
            m_NativeAppleStore.FetchStorePromotionOrder();
        }
    }
}
