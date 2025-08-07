#nullable enable

using System;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class FetchStorePromotionVisibilityUseCase : IFetchStorePromotionVisibilityUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;
        readonly INativeAppleStore m_NativeAppleStore;

        [Preserve]
        internal FetchStorePromotionVisibilityUseCase(IAppleStoreCallbacks appleStoreCallbacks, INativeAppleStore nativeStore)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
            m_NativeAppleStore = nativeStore;
        }

        public void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)
        {
            m_AppleStoreCallbacks.SetFetchStorePromotionVisibilityCallbacks(successCallback, errorCallback);
            m_NativeAppleStore.FetchStorePromotionVisibility(product.definition.storeSpecificId);
        }
    }
}
