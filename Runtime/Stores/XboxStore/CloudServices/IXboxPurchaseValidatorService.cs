#if IAP_GDK && MICROSOFT_GDK_SUPPORT && (UNITY_EDITOR || UNITY_GAMECORE || UNITY_STANDALONE_WIN) && IAP_CLOUDCODE_ENABLED
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
