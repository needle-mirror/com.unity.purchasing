#if IAP_GDK && MICROSOFT_GDK_SUPPORT && IAP_CLOUDCODE_ENABLED
using Unity.XGamingRuntime;

namespace UnityEngine.Purchasing
{
    public delegate void XboxPurchaseValidationCallback(bool validated, string errorMessage);

    public interface IXboxPurchaseValidatorService
    {
        public void ValidatePurchaseAsync(XStoreContext storeContext, XUserHandle userHandle, XboxCloudSettings settings, string storeSpecificId, XboxPurchaseValidationCallback callback);
    }
}
#endif
