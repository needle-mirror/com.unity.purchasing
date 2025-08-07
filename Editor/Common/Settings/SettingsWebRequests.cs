using System;
using System.Threading.Tasks;
using Unity.Services.Core.Editor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Handles web requests for fetching In-App Purchase settings from Unity services.
    /// This class manages the communication with Unity's IAP configuration endpoints to retrieve project-specific settings.
    /// </summary>
    public class SettingsWebRequests
    {
        readonly Action<IapSettings> GetIAPSettingsCallback;
        UnityWebRequest GetIAPSettingsRequest;

        internal SettingsWebRequests(Action<IapSettings> onGetIAPSettings)
        {
            GetIAPSettingsCallback = onGetIAPSettings;
            var _ = GetIAPSettingsAsync();
        }

        /// <summary>
        /// Finalizer that ensures proper cleanup of web request resources.
        /// Aborts and disposes any active web requests to prevent resource leaks.
        /// </summary>
        ~SettingsWebRequests()
        {
            GetIAPSettingsRequest?.Abort();
            GetIAPSettingsRequest?.Dispose();
            GetIAPSettingsRequest = null;
        }

        async Task<string> GetGatewayAccessTokenAsync()
        {
            var authToken = new AccessTokens();
            return await authToken.GetServicesGatewayTokenAsync();
        }

        internal async Task GetIAPSettingsAsync()
        {
            var accessToken = await GetGatewayAccessTokenAsync();
            if (GetIAPSettingsRequest != null) return;

            BuildGetIAPSettingsRequest(accessToken);

            var operation = GetIAPSettingsRequest.SendWebRequest();
            operation.completed += OnGetIAPSettings;
        }

        void BuildGetIAPSettingsRequest(string accessToken)
        {
            var settingsEndpoint = string.Format(IapSettingsConsts.SettingsEndpoint, CloudProjectSettings.projectId);
            GetIAPSettingsRequest = UnityWebRequest.Get(GetIAPApiPath() + settingsEndpoint);
            GetIAPSettingsRequest.suppressErrorsToConsole = true;

            AddAccessTokenToRequestHeader(GetIAPSettingsRequest, accessToken);
            AddClientIdToRequestHeader(GetIAPSettingsRequest);
        }

        static void AddAccessTokenToRequestHeader(UnityWebRequest request, string accessToken)
        {
            request.SetRequestHeader("Authorization", string.Format(IapSettingsConsts.AuthorizationHeaderValue, accessToken));
        }

        static void AddClientIdToRequestHeader(UnityWebRequest request)
        {
            request.SetRequestHeader("x-client-id", IapSettingsConsts.XClientIdHeader);
        }

        void OnGetIAPSettings(AsyncOperation getIAPSettingsOperation)
        {
            var webOp = (UnityWebRequestAsyncOperation)getIAPSettingsOperation;

            if (webOp?.isDone == true && GetIAPSettingsRequest != null)
            {
                FetchIAPSettings();

                GetIAPSettingsRequest.Dispose();
                GetIAPSettingsRequest = null;
            }
        }

        void FetchIAPSettings()
        {
            if (!GetIAPSettingsRequest.IsResultTransferSuccess())
            {
                Debug.LogError($"Cannot fetch IAP settings : {GetIAPSettingsRequest.error}");
                return;
            }

            try
            {
                var settings = JsonUtility.FromJson<IapSettings>(GetIAPSettingsRequest.downloadHandler.text);
                GetIAPSettingsCallback(settings);
            }
            catch (Exception ex)
            {
                Debug.unityLogger.LogIAPException(ex);
            }
        }

        string GetIAPApiPath() =>
            $"{IapSettingsConsts.ProductionPath}{IapSettingsConsts.ApiPath}";
    }
}
