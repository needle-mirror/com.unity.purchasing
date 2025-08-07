#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a failed store connection as described by a store service.
    /// </summary>
    [Serializable]
    public class StoreConnectionFailureDescription : IRetryableRequestFailureDescription
    {
        /// <summary>
        /// The message containing details about the failed connection attempt.
        /// </summary>
        public string message;

        /// <summary>
        /// Specifies if the connection request can be retried.
        /// </summary>
        public bool isRetryable;

        /// <summary>
        /// The message containing details about the failed connection attempt.
        /// </summary>
        public string Message => message;

        /// <summary>
        /// Specifies if the request is retryable
        /// </summary>
        public bool IsRetryable => isRetryable;

        /// <summary>
        /// Parametrized Constructor.
        /// </summary>
        /// <param name="message"> The message containing details about the failed connection attempt. </param>
        /// <param name="isRetryable"> Specifies if the query can be retried. </param>
        public StoreConnectionFailureDescription(string message, bool isRetryable = false)
        {
            this.message = message;
            this.isRetryable = isRetryable;
        }
    }
}
