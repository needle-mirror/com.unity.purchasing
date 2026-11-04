#if IAP_GDK && MICROSOFT_GDK_SUPPORT && (UNITY_EDITOR || UNITY_GAMECORE || UNITY_STANDALONE_WIN)
using System.Collections.Generic;
using Unity.XGamingRuntime;

namespace UnityEngine.Purchasing
{
    delegate void XboxQueryEntitlementsCallback(int hResult, List<XStoreProduct> entitledProducts, ProductDefinition productToCheck);

    interface IXboxQueryEntitlementsService
    {
        void QueryEntitlementsAsync(XStoreContext storeContext, XStoreProductKind productKinds, ProductDefinition productToCheck, XboxQueryEntitlementsCallback callback);
    }
}
#endif
