#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class XboxFetchProductsService : IXboxFetchProductsService
    {
        public void FetchAvailableProducts(XStoreContext storeContext, XStoreProductKind productKinds, XboxFetchProductsCallback callback)
        {
            SDK.XStoreQueryAssociatedProductsAsync(
                storeContext,
                productKinds,
                Int32.MaxValue,
                (int hResult, XStoreQueryResult result) =>
                {
                    FetchAvailableProductsResults(hResult, result, new List<XStoreProduct>(), callback);
                });
        }

        private void FetchAvailableProductsResults(int hResult, XStoreQueryResult result, List<XStoreProduct> xStoreProducts, XboxFetchProductsCallback callback)
        {
            if (HR.FAILED(hResult))
            {
                callback?.Invoke(hResult, xStoreProducts);
                return;
            }

            xStoreProducts.AddRange(result.PageItems);
            if (result.HasMorePages)
            {
                SDK.XStoreProductsQueryNextPageAsync(result, (int nextHResult, XStoreQueryResult nextResult) =>
                {
                    FetchAvailableProductsResults(nextHResult, nextResult, xStoreProducts, callback);
                });
            }
            else
            {
                callback?.Invoke(hResult, xStoreProducts);
            }
        }

        public void FetchProducts(XStoreContext storeContext, XStoreProductKind productKinds, string[] productIds, XboxFetchProductsCallback callback)
        {
            SDK.XStoreQueryProductsAsync(storeContext, productKinds, productIds, null,
                (int hResult, XStoreQueryResult result) =>
                {
                    FetchProductsResults(hResult, result, new List<XStoreProduct>(), callback);
                });
        }

        private void FetchProductsResults(int hResult, XStoreQueryResult result, List<XStoreProduct> xStoreProducts, XboxFetchProductsCallback callback)
        {
            if (HR.FAILED(hResult))
            {
                callback?.Invoke(hResult, xStoreProducts);
                return;
            }

            xStoreProducts.AddRange(result.PageItems);
            if (result.HasMorePages)
            {
                SDK.XStoreProductsQueryNextPageAsync(result, (int nextHResult, XStoreQueryResult nextResult) =>
                {
                    FetchProductsResults(nextHResult, nextResult, xStoreProducts, callback);
                });
            }
            else
            {
                callback?.Invoke(hResult, xStoreProducts);
            }
        }
    }
}
#endif
