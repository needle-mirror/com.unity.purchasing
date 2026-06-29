using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Unity.Purchasing.Editor.Shared.WebApi.Network
{
    static class WebRequestUtils
    {
        public static void SendWebRequest(UnityWebRequest request, Action<ApiResponse> callback, CancellationToken cancellationToken)
        {
            var requestOperation = request.SendWebRequest();
            var registration = cancellationToken.Register(() => Abort(request));
            requestOperation.completed += _ => OnCompleted(callback, request, registration);
        }

        public static void SendWebRequest<T>(UnityWebRequest request, Action<ApiResponse<T>> callback, CancellationToken cancellationToken)
        {
            var requestOperation = request.SendWebRequest();
            var registration = cancellationToken.Register(() => Abort(request));
            requestOperation.completed += _ => OnCompleted(callback, request, registration);
        }

        public static Task<ApiResponse> SendWebRequestAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<ApiResponse>();
            var requestOperation = request.SendWebRequest();
            var registration = cancellationToken.Register(() => Abort(request));
            requestOperation.completed += _ => OnCompleted(tcs, request, registration);
            return tcs.Task;
        }

        public static Task<ApiResponse<T>> SendWebRequestAsync<T>(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<ApiResponse<T>>();
            var requestOperation = request.SendWebRequest();
            var registration = cancellationToken.Register(() => Abort(request));
            requestOperation.completed += _ => OnCompleted(tcs, request, registration);
            return tcs.Task;
        }

        static void Abort(UnityWebRequest request)
        {
            if (!request.isDone)
            {
                request.Abort();
            }
        }

        static void OnCompleted(Action<ApiResponse> callback, UnityWebRequest request, CancellationTokenRegistration registration)
        {
            var response = new ApiResponse();
            ProcessResponse(request, response);
            registration.Dispose();
            request.Dispose();
            callback.Invoke(response);
        }

        static void OnCompleted<T>(Action<ApiResponse<T>> callback, UnityWebRequest request, CancellationTokenRegistration registration)
        {
            var response = new ApiResponse<T>();
            ProcessObjectResponse(request, response);
            registration.Dispose();
            request.Dispose();
            callback.Invoke(response);
        }

        static void OnCompleted(TaskCompletionSource<ApiResponse> tcs, UnityWebRequest request, CancellationTokenRegistration registration)
        {
            var response = new ApiResponse();
            ProcessResponse(request, response);
            registration.Dispose();
            request.Dispose();
            tcs.TrySetResult(response);
        }

        static void OnCompleted<T>(TaskCompletionSource<ApiResponse<T>> tcs, UnityWebRequest request, CancellationTokenRegistration registration)
        {
            var response = new ApiResponse<T>();
            ProcessObjectResponse(request, response);
            registration.Dispose();
            request.Dispose();
            tcs.TrySetResult(response);
        }

        static void ProcessResponse(UnityWebRequest request, ApiResponse response)
        {
            response.Url = request.url;
            response.StatusCode = (int)request.responseCode;
            response.ErrorText = request.error;
            response.Headers = request.GetResponseHeaders();
            response.Content = request.downloadHandler?.text;

            if (!IsSuccessful(request))
            {
                if (IsClientError(request))
                {
                    response.ErrorType = ApiErrorType.Http;
                }
                else if (IsServerError(request))
                {
                    response.ErrorType = ApiErrorType.Network;
                }
                else
                {
                    response.ErrorType = ApiErrorType.Canceled;
                }
            }
        }

        static void ProcessObjectResponse<T>(UnityWebRequest request, ApiResponse<T> response)
        {
            ProcessResponse(request, response);

            if (IsSuccessful(request))
            {
                try
                {
                    var data = request.downloadHandler?.text;

                    if (!string.IsNullOrEmpty(data))
                    {
                        response.Data = JsonConvert.DeserializeObject<T>(data);
                    }
                }
                catch (Exception e)
                {
                    response.ErrorType = ApiErrorType.Deserialization;
                    response.ErrorText = $"Deserialization of type '{typeof(T)}' failed.\n{e}";
                }
            }
        }

        static bool IsSuccessful(UnityWebRequest request)
        {
            return request.responseCode >= 200 && request.responseCode < 300;
        }

        static bool IsClientError(UnityWebRequest request)
        {
            return request.responseCode >= 400 && request.responseCode < 500;
        }

        static bool IsServerError(UnityWebRequest request)
        {
            return request.responseCode >= 500;
        }
    }
}
