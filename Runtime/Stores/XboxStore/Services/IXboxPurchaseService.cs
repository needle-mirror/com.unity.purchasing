#if IAP_GDK && MICROSOFT_GDK_SUPPORT
using System;
using Unity.XGamingRuntime;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    delegate void XboxShowPurchaseUICallback(int hResult, string storeSpecificId);
    delegate void XboxFulfillConsumableCallback(int hResult, Product product, Guid trackingId, XStoreConsumableResult result);

    interface IXboxPurchaseService
    {
        void ShowPurchaseUIAsync(XStoreContext storeContext, string storeSpecificId, XboxShowPurchaseUICallback callback);
        void FulfillConsumableAsync(XStoreContext storeContext, Product product, Guid trackingId, XboxFulfillConsumableCallback callback);
    }
}
#endif
