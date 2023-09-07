using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core.Editor;

namespace UnityEditor.Purchasing
{
    class GoogleConfigurationWebRequests
    {
        readonly Action<string, GooglePlayRevenueTrackingKeyState> m_GetGooglePlayKeyCallback;
        IAccessTokens m_CoreAccessTokens;

        internal GoogleConfigurationWebRequests(Action<string, GooglePlayRevenueTrackingKeyState> onGetGooglePlayKey)
        {
            m_GetGooglePlayKeyCallback = onGetGooglePlayKey;
            m_CoreAccessTokens = new AccessTokens();
        }

        internal void RequestRetrieveKeyOperation()
        {
            GetGatewayTokenAndThenRetrieveGooglePlayKey();
        }

        async void GetGatewayTokenAndThenRetrieveGooglePlayKey()
        {
            var gatewayToken = await m_CoreAccessTokens.GetServicesGatewayTokenAsync();
            if (!string.IsNullOrEmpty(gatewayToken))
            {
                GetGooglePlayKey(gatewayToken);
            }
            else
            {
                m_GetGooglePlayKeyCallback(null, GooglePlayRevenueTrackingKeyState.ServerError);
            }
        }

        async void GetGooglePlayKey(string gatewayToken)
        {
            var googlePlayKeyResult = await GetGoogleKeyWebRequest.RequestGooglePlayKeyAsync(gatewayToken);
            ReportGooglePlayKeyAndTrackingState(googlePlayKeyResult.GooglePlayKey, googlePlayKeyResult.ResponseCode);
        }

        void ReportGooglePlayKeyAndTrackingState(string googlePlayKey, long responseCode)
        {
            var trackingState = InterpretKeyStateFromProtocolError(responseCode);

            if (trackingState == GooglePlayRevenueTrackingKeyState.Verified && string.IsNullOrEmpty(googlePlayKey))
            {
                trackingState = GooglePlayRevenueTrackingKeyState.InvalidFormat;
            }

            m_GetGooglePlayKeyCallback(googlePlayKey, trackingState);

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
                case 404:
                    return GooglePlayRevenueTrackingKeyState.CantFetch;
                case 405:
                case 500:
                    return GooglePlayRevenueTrackingKeyState.ServerError;
                default:
                    return GooglePlayRevenueTrackingKeyState.CantFetch; //Could instead use a generic unknown message, but this is good enough.
            }
        }
    }
}
