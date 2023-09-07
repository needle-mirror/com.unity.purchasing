using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Purchasing
{
    class GetGoogleKeyWebRequest
    {
        const string k_GoogleJsonLabel = "google";
        const string k_PublicKeyJsonLabel = "publicKey";

        const string k_AuthHeaderName = "Authorization";
        const string k_AuthHeaderValueFormat = "Bearer {0}";

        internal static async Task<GooglePlayKeyRequestResult> RequestGooglePlayKeyAsync(string gatewayToken)
        {
            var response = await SendUnityWebRequestAndGetResponseAsync(gatewayToken);
            return response;
        }

        static async Task<GooglePlayKeyRequestResult> SendUnityWebRequestAndGetResponseAsync(string gatewayToken)
        {
            using (var request = await CreateAndSendWebRequestAsync(gatewayToken))
            {
                var requestResult = new GooglePlayKeyRequestResult();

                if (request.IsResultTransferSuccess())
                {
                    requestResult.GooglePlayKey = FetchGooglePlayKeyFromRequest(request.downloadHandler.text);
                }
                else
                {
                    requestResult.GooglePlayKey = "";
                }

                requestResult.ResponseCode = request.responseCode;

                return requestResult;
            }
        }

        static Task<UnityWebRequest> CreateAndSendWebRequestAsync(string gatewayToken)
        {
            var taskCompletionSource = new TaskCompletionSource<UnityWebRequest>();

            var operation = BuildUnityWebRequest(gatewayToken).SendWebRequest();
            operation.completed += OnRequestCompleted;

            return taskCompletionSource.Task;

            void OnRequestCompleted(UnityEngine.AsyncOperation operation)
            {
                var request = ((UnityWebRequestAsyncOperation)operation).webRequest;
                using (request)
                {
                    taskCompletionSource.TrySetResult(request);
                }
            }
        }

        static UnityWebRequest BuildUnityWebRequest(string gatewayToken)
        {
            var url = string.Format(PurchasingUrls.iapSettingssUrl, CloudProjectSettings.projectId);
            var request = UnityWebRequest.Get(url);
            request.suppressErrorsToConsole = true;

            request.SetRequestHeader(k_AuthHeaderName, string.Format(k_AuthHeaderValueFormat, gatewayToken));
            return request;
        }

        static string FetchGooglePlayKeyFromRequest(string downloadedText)
        {
            var googlePlayKey = "";
            try
            {
                var innerBlock = NetworkingUtils.GetJsonDictionaryWithinRawJsonDictionaryString(downloadedText, k_GoogleJsonLabel);
                googlePlayKey = NetworkingUtils.GetStringFromJsonDictionary(innerBlock, k_PublicKeyJsonLabel);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return googlePlayKey;
        }
    }
}
