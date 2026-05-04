#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System.Collections.Generic;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    delegate void XboxQueryEntitlementsCallback(int hResult, List<XStoreProduct> entitledProducts, ProductDefinition productToCheck);

    interface IXboxQueryEntitlementsService
    {
        void QueryEntitlementsAsync(XStoreContext storeContext, XStoreProductKind productKinds, ProductDefinition productToCheck, XboxQueryEntitlementsCallback callback);
    }
}
#endif
