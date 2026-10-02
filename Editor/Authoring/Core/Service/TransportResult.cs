using System.Collections.Generic;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    /// <summary>
    /// The HTTP envelope shared by every transport result: the status outcome, headers, and,
    /// on failure, the error body. The retry/backoff machinery depends only on these fields,
    /// so it can operate over any result regardless of its payload type.
    /// </summary>
    internal interface ITransportResult
    {
        /// <summary>Status code of the async request response.</summary>
        int StatusCode { get; }

        /// <summary>Quick accessor for successful requests.</summary>
        bool IsSuccess { get; }

        /// <summary>Response headers (e.g. <c>Retry-After</c>).</summary>
        IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Error body/message. <c>null</c> on a successful result.</summary>
        string Error { get; }
    }

    /// <summary>
    /// Result for transport operations that carry no success payload (e.g. delete). The status
    /// code and headers are always present; <see cref="Error"/> carries the body on failure.
    /// </summary>
    internal readonly struct TransportResult : ITransportResult
    {
        public int StatusCode { get; }
        public bool IsSuccess => StatusCode is >= 200 and < 300;
        public IReadOnlyDictionary<string, string> Headers { get; }
        public string Error { get; }

        public TransportResult(
            int statusCode,
            string errorText = null,
            IReadOnlyDictionary<string, string> headers = null)
        {
            StatusCode = statusCode;
            Error = errorText;
            Headers = headers;
        }
    }

    /// <summary>
    /// Result carrying a strongly-typed <typeparamref name="T"/> payload. <see cref="Content"/>
    /// is meaningful only when <see cref="IsSuccess"/> is <c>true</c>; on failure it is
    /// <c>default</c> and the error body rides <see cref="Error"/> instead.
    /// </summary>
    /// <typeparam name="T">The parsed domain payload for a successful response.</typeparam>
    internal readonly struct TransportResult<T> : ITransportResult
    {
        public int StatusCode { get; }
        public bool IsSuccess => StatusCode is >= 200 and < 300;
        public IReadOnlyDictionary<string, string> Headers { get; }
        public string Error { get; }

        /// <summary>Deserialized success payload. <c>default</c> when <see cref="IsSuccess"/> is false.</summary>
        public T Content { get; }

        public TransportResult(
            int statusCode,
            T content = default,
            IReadOnlyDictionary<string, string> headers = null,
            string error = null)
        {
            StatusCode = statusCode;
            Content = content;
            Headers = headers;
            Error = error;
        }
    }
}
