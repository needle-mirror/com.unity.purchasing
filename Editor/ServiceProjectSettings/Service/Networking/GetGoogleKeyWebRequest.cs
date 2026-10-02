using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    class GetGoogleKeyWebRequest
    {
        const string k_GoogleJsonLabel = "google";
        const string k_PublicKeyJsonLabel = "publicKey";

        const string k_AuthHeaderName = "Authorization";
        const string k_AuthHeaderValueFormat = "Bearer {0}";

        internal static async Task<GooglePlayKeyRequestResult> RequestGooglePlayKeyAsync(string gatewayToken, string projectId)
        {
            var response = await SendUnityWebRequestAndGetResponseAsync(gatewayToken, projectId);
            return response;
        }

        internal static async Task<long> PushGooglePlayKeyAsync(string gatewayToken, string projectId, string googlePlayKey)
        {
            var body = JsonUtility.ToJson(new IapSettings { google = new GoogleIapSettings { publicKey = googlePlayKey } });

            var request = BuildUnityWebRequest(gatewayToken, projectId, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.SetRequestHeader("Content-Type", "application/json");

            using (var response = await SendWebRequestAsync(request))
            {
                return response.responseCode;
            }
        }

        static async Task<GooglePlayKeyRequestResult> SendUnityWebRequestAndGetResponseAsync(string gatewayToken, string projectId)
        {
            using (var request = await SendWebRequestAsync(BuildUnityWebRequest(gatewayToken, projectId, UnityWebRequest.kHttpVerbGET)))
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

        static Task<UnityWebRequest> SendWebRequestAsync(UnityWebRequest webRequest)
        {
            var taskCompletionSource = new TaskCompletionSource<UnityWebRequest>();

            var operation = webRequest.SendWebRequest();
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

        // The project id is passed in, not read here, so a request can't land on a project linked after it started.
        static UnityWebRequest BuildUnityWebRequest(string gatewayToken, string projectId, string method)
        {
            var url = string.Format(PurchasingUrls.iapSettingssUrl, projectId);
            var request = new UnityWebRequest(url, method, new DownloadHandlerBuffer(), null);
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
