#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System;
using System.Collections.Generic;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class XboxQueryEntitlementsService : IXboxQueryEntitlementsService
    {
        public void QueryEntitlementsAsync(XStoreContext storeContext, XStoreProductKind productKinds, ProductDefinition productToCheck, XboxQueryEntitlementsCallback callback)
        {
            SDK.XStoreQueryEntitledProductsAsync(
                storeContext,
                productKinds,
                Int32.MaxValue, // Don't need paging
                (int hResult, XStoreQueryResult result) =>
                {
                    ProcessEntitledProductsResults(hResult, result, new List<XStoreProduct>(), productToCheck, callback);
                });
        }

        private void ProcessEntitledProductsResults(int hResult, XStoreQueryResult result, List<XStoreProduct> entitledProducts, ProductDefinition productToCheck, XboxQueryEntitlementsCallback callback)
        {
            if (HR.FAILED(hResult))
            {
                callback?.Invoke(hResult, entitledProducts, productToCheck);
                return;
            }

            entitledProducts.AddRange(result.PageItems);
            if (result.HasMorePages)
            {
                SDK.XStoreProductsQueryNextPageAsync(result,
                    (int nextHResult, XStoreQueryResult nextResult) =>
                    {
                        // Continue building the entitledProducts list
                        ProcessEntitledProductsResults(nextHResult, nextResult, entitledProducts, productToCheck, callback);
                    });
            }
            else
            {
                callback?.Invoke(hResult, entitledProducts, productToCheck);
            }
        }
    }
}
#endif
