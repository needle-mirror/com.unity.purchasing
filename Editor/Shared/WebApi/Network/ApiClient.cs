using System.Text;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Unity.Purchasing.Editor.Shared.WebApi.Network
{
    /// <inheritdoc/>
    class ApiClient : IApiClient
    {
        internal const string k_MethodGet = "GET";
        internal const string k_MethodHead = "HEAD";
        internal const string k_MethodPost = "POST";
        internal const string k_MethodPut = "PUT";
        internal const string k_MethodPatch = "PATCH";
        internal const string k_MethodOptions = "OPTIONS";
        internal const string k_MethodDelete = "DELETE";

        internal IRetryPolicy RetryPolicy { get; }

        /// <summary>
        /// Creates an api client responsible for network operations
        /// </summary>
        /// <param name="retryPolicy">The retry policy</param>
        public ApiClient(IRetryPolicy retryPolicy = null)
        {
            RetryPolicy = retryPolicy ?? new RetryPolicy();
        }

        /// <inheritdoc/>
        public ApiOperation Get(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodGet, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Get<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodGet, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Post(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodPost, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Post<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodPost, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Put(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodPut, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Put<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodPut, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Delete(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodDelete, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Delete<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodDelete, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Head(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodHead, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Head<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodHead, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Options(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodOptions, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Options<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodOptions, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation Patch(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation(), path, k_MethodPatch, options, configuration, cancellationToken);
        }

        /// <inheritdoc/>
        public ApiOperation<T> Patch<T>(string path, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken)
        {
            return Send(new ApiOperation<T>(), path, k_MethodPatch, options, configuration, cancellationToken);
        }

        ApiOperation Send(ApiOperation operation, string path, string method, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken = default, int attempt = 0)
        {
            WebRequestUtils.SendWebRequest(BuildWebRequest(path, method, options, configuration), async(response) =>
            {
                if (response.IsSuccessful)
                {
                    operation.Complete(response);
                    return;
                }

                if (RetryPolicy.Policy != null)
                {
                    if (!await RetryPolicy.Policy.Invoke(response, attempt))
                    {
                        operation.Complete(response);
                        return;
                    }

                    await Send(operation, path, method, options, configuration, cancellationToken, attempt + 1);
                }

                operation.Complete(response);
            }, cancellationToken);

            return operation;
        }

        ApiOperation<T> Send<T>(ApiOperation<T> operation, string path, string method, ApiRequestOptions options, IApiConfiguration configuration, CancellationToken cancellationToken = default, int attempt = 0)
        {
            WebRequestUtils.SendWebRequest<T>(BuildWebRequest(path, method, options, configuration), async(response) =>
            {
                if (response.IsSuccessful)
                {
                    operation.Complete(response);
                    return;
                }

                if (RetryPolicy.Policy != null)
                {
                    if (!await RetryPolicy.Policy.Invoke(response, attempt))
                    {
                        operation.Complete(response);
                        return;
                    }

                    await Send(operation, path, method, options, configuration, cancellationToken, attempt + 1);
                }

                operation.Complete(response);
            }, cancellationToken);

            return operation;
        }

        static UnityWebRequest BuildWebRequest(string path, string method, ApiRequestOptions options, IApiConfiguration configuration)
        {
            var builder = new ApiRequestPathBuilder(configuration.BasePath, path);
            builder.AddPathParameters(options.PathParameters);
            builder.AddQueryParameters(options.QueryParameters);
            var uri = builder.GetFullUri();

            var request = new UnityWebRequest(uri, method);

            if (configuration.UserAgent != null)
            {
                request.SetRequestHeader("User-Agent", configuration.UserAgent);
            }

            if (configuration.DefaultHeaders != null)
            {
                foreach (var headerParam in configuration.DefaultHeaders)
                {
                    request.SetRequestHeader(headerParam.Key, headerParam.Value);
                }
            }

            if (options.HeaderParameters != null)
            {
                foreach (var headerParam in options.HeaderParameters)
                {
                    foreach (var value in headerParam.Value)
                    {
                        request.SetRequestHeader(headerParam.Key, value);
                    }
                }
            }

            request.timeout = configuration.Timeout;

            if (options.Data != null)
            {
                var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                var data = JsonConvert.SerializeObject(options.Data, settings);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            return request;
        }
    }
}
