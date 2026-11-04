#if IAP_GDK && MICROSOFT_GDK_SUPPORT && (UNITY_EDITOR || UNITY_GAMECORE || UNITY_STANDALONE_WIN)
using System.Collections.Generic;
using Unity.XGamingRuntime;

namespace UnityEngine.Purchasing
{
    delegate void XboxFetchProductsCallback(int hResult, List<XStoreProduct> products);

    interface IXboxFetchProductsService
    {
        void FetchAvailableProducts(XStoreContext storeContext, XStoreProductKind productKinds, XboxFetchProductsCallback callback);
        void FetchProducts(XStoreContext storeContext, XStoreProductKind productKinds, string[] productIds, XboxFetchProductsCallback callback);
    }
}
#endif
