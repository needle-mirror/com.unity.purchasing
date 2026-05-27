#if IAP_GDK && MICROSOFT_GDK_SUPPORT && IAP_CLOUDCODE_ENABLED
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.XGamingRuntime;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    public class XboxPurchaseValidatorService : IXboxPurchaseValidatorService
    {
        public void ValidatePurchaseAsync(XStoreContext storeContext, XUserHandle userHandle, XboxCloudSettings settings, string storeSpecificId, XboxPurchaseValidationCallback callback)
        {
            if (string.IsNullOrEmpty(settings.CloudCodeModuleName)
                || string.IsNullOrEmpty(settings.ServiceTicketFunctionName)
                || string.IsNullOrEmpty(settings.ValidatePurchaseFunctionName))
            {
                callback?.Invoke(false, "Purchase validation module is not configured properly.");
                return;
            }
            else if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
            {
                callback?.Invoke(false, "Purchase validation requires authentication. Sign in via AuthenticationService before validating.");
                return;
            }

            GetCollectionsId(storeContext, settings, storeSpecificId, callback);
        }

        private async void GetCollectionsId(XStoreContext storeContext, XboxCloudSettings settings, string storeSpecificId, XboxPurchaseValidationCallback callback)
        {
            try
            {
                var serviceTicket = await CloudCodeService.Instance.CallModuleEndpointAsync<string>(
                    settings.CloudCodeModuleName,
                    settings.ServiceTicketFunctionName,
                    null);
                SDK.XStoreGetUserCollectionsIdAsync(storeContext, serviceTicket, null,
                    (int collectionsHResult, string collectionsId) =>
                        OnCollectionsIdRetrieved(settings, collectionsHResult, storeSpecificId, collectionsId, callback));
            }
            catch (CloudCodeException e)
            {
                callback?.Invoke(false, $"Purchase validation failed: hr=0x{e.ErrorCode:X} {e.Message}");
                return;
            }
            catch (Exception e)
            {
                callback?.Invoke(false, $"Purchase validation failed: {e.Message}");
                return;
            }
        }

        private async void OnCollectionsIdRetrieved(XboxCloudSettings settings, int hResult, string storeSpecificId, string collectionsId, XboxPurchaseValidationCallback callback)
        {
            if (HR.FAILED(hResult))
            {
                callback?.Invoke(false, $"Purchase validation failed, could not get User Collections ID. hr=0x{hResult:X}");
                return;
            }

            var valid = false;
            try
            {
                var args = new Dictionary<string, object>
                    {
                        { "collectionsIdToken", collectionsId },
                        { "productIds", new List<string> { storeSpecificId } }
                    };
                var hr = SDK.XSystemGetXboxLiveSandboxId(out var sandboxId);
                if (HR.SUCCEEDED(hr) && !string.IsNullOrEmpty(sandboxId))
                {
                    args.Add("sandboxId", sandboxId);
                }

                valid = await CloudCodeService.Instance.CallModuleEndpointAsync<bool>(
                    settings.CloudCodeModuleName,
                    settings.ValidatePurchaseFunctionName,
                    args);
            }
            catch (CloudCodeException e)
            {
                callback?.Invoke(false, $"Purchase validation failed: hr=0x{e.ErrorCode:X} {e.Message}");
                return;
            }
            catch (Exception e)
            {
                callback?.Invoke(false, $"Purchase validation failed: {e.Message}");
                return;
            }

            callback?.Invoke(valid, valid ? null : "Purchase is not valid.");
        }
    }
}
#endif
