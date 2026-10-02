using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core.Editor;

namespace UnityEditor.Purchasing
{
    class GoogleConfigurationWebRequests
    {
        // Reported by a push when the editor was relinked before the key was sent; nothing was written.
        internal const long k_PushCancelledProjectChanged = -1;

        readonly Action<string, GooglePlayRevenueTrackingKeyState> m_GetGooglePlayKeyCallback;
        IAccessTokens m_CoreAccessTokens;

        internal GoogleConfigurationWebRequests(Action<string, GooglePlayRevenueTrackingKeyState> onGetGooglePlayKey)
        {
            m_GetGooglePlayKeyCallback = onGetGooglePlayKey;
            m_CoreAccessTokens = new AccessTokens();
        }

        // async void, so nothing may escape: an exception would surface in the Console with no caller to handle it.
        internal async void RequestRetrieveKeyOperation()
        {
            var projectId = CloudProjectSettings.projectId;
            string key = null;
            GooglePlayRevenueTrackingKeyState state;
            try
            {
                (key, state) = await RetrieveGooglePlayKey(projectId);
            }
            catch (Exception)
            {
                // Core's token exchange throws when offline or signed out; same outcome as a failed request.
                state = GooglePlayRevenueTrackingKeyState.CantFetch;
            }

            // A relink while this was in flight means the result describes a project that is no longer linked.
            if (CloudProjectSettings.projectId == projectId)
            {
                m_GetGooglePlayKeyCallback(key, state);
            }
        }

        internal async void RequestPushKeyOperation(string googlePlayKey, Action<long> onPushed)
        {
            // The key was confirmed for the project linked at click time; never send it anywhere else.
            var projectId = CloudProjectSettings.projectId;
            long responseCode;
            try
            {
                var gatewayToken = await m_CoreAccessTokens.GetServicesGatewayTokenAsync();
                if (CloudProjectSettings.projectId != projectId)
                {
                    responseCode = k_PushCancelledProjectChanged;
                }
                else
                {
                    responseCode = string.IsNullOrEmpty(gatewayToken)
                        ? 401
                        : await GetGoogleKeyWebRequest.PushGooglePlayKeyAsync(gatewayToken, projectId, googlePlayKey);
                }
            }
            catch (Exception)
            {
                // No response at all; reported as "Couldn't reach Unity services".
                responseCode = 0;
            }

            onPushed(responseCode);
        }

        async Task<(string, GooglePlayRevenueTrackingKeyState)> RetrieveGooglePlayKey(string projectId)
        {
            var gatewayToken = await m_CoreAccessTokens.GetServicesGatewayTokenAsync();
            if (string.IsNullOrEmpty(gatewayToken))
            {
                return (null, GooglePlayRevenueTrackingKeyState.ServerError);
            }

            var result = await GetGoogleKeyWebRequest.RequestGooglePlayKeyAsync(gatewayToken, projectId);
            return (result.GooglePlayKey, InterpretKeyState(result.ResponseCode, result.GooglePlayKey));
        }

        internal static GooglePlayRevenueTrackingKeyState InterpretKeyState(long responseCode, string googlePlayKey)
        {
            var trackingState = InterpretKeyStateFromProtocolError(responseCode);

            // Settings exist for the project but no Google key was ever entered.
            if (trackingState == GooglePlayRevenueTrackingKeyState.Verified && string.IsNullOrEmpty(googlePlayKey))
            {
                trackingState = GooglePlayRevenueTrackingKeyState.NoKey;
            }

            return trackingState;
        }

        static GooglePlayRevenueTrackingKeyState InterpretKeyStateFromProtocolError(long responseCode)
        {
            switch (responseCode)
            {
                case 200:
                    return GooglePlayRevenueTrackingKeyState.Verified;
                case 401:
                case 403:
                    return GooglePlayRevenueTrackingKeyState.UnauthorizedUser;
                case 400:
                    return GooglePlayRevenueTrackingKeyState.InvalidFormat;
                case 404:
                    // iap-settings answers 404 when the project has never had settings saved.
                    return GooglePlayRevenueTrackingKeyState.NoKey;
                case 405:
                case 500:
                    return GooglePlayRevenueTrackingKeyState.ServerError;
                default:
                    return GooglePlayRevenueTrackingKeyState.CantFetch; //Could instead use a generic unknown message, but this is good enough.
            }
        }
    }
}
