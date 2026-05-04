#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class XboxPurchaseService : IXboxPurchaseService
    {
        // Developer-Managed Consumables should be fulfilled one at a time.
        private const int k_FulfillQuantity = 1;

        public void ShowPurchaseUIAsync(XStoreContext storeContext, string storeSpecificId, XboxShowPurchaseUICallback callback)
        {
            SDK.XStoreShowPurchaseUIAsync(
                storeContext,
                storeSpecificId,
                null,
                null,
                (int hResult) =>
                {
                    callback(hResult, storeSpecificId);
                });
        }

        public void FulfillConsumableAsync(XStoreContext storeContext, Product product, Guid trackingId, XboxFulfillConsumableCallback callback)
        {
            SDK.XStoreReportConsumableFulfillmentAsync(storeContext, product.definition.storeSpecificId, k_FulfillQuantity, trackingId, (int hResult, XStoreConsumableResult result) =>
            {
                callback?.Invoke(hResult, product, trackingId, result);
            });
        }
    }
}
#endif
