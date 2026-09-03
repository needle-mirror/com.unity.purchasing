using System.Collections.Generic;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    /// <summary>
    /// Normalised result returned by <see cref="ILiveContentApiTransport"/> methods.
    /// Carries the HTTP status code, raw response body, and headers
    /// </summary>
    internal readonly struct TransportResult
    {
        /// <summary>
        /// Status code of the async request response
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Quick accessor for successful requests
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        /// <summary>
        /// Json content for the request body
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Headers for the request
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Constructor for <see cref="TransportResult"/>
        /// </summary>
        /// <param name="statusCode">Status/Error code</param>
        /// <param name="content">Json content body</param>
        /// <param name="headers">Request headers</param>
        public TransportResult(
            int statusCode,
            string content = null,
            IReadOnlyDictionary<string, string> headers = null)
        {
            StatusCode = statusCode;
            Content = content;
            Headers = headers;
        }
    }
}
