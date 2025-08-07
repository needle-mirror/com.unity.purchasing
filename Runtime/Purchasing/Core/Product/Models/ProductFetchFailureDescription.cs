#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Represents a failed product fetch as described by a product service.
    /// </summary>
    [Serializable]
    public class ProductFetchFailureDescription : IRetryableRequestFailureDescription
    {
        /// <summary>
        /// The reason for the failure of the product fetch.
        /// </summary>
        public ProductFetchFailureReason reason;
        /// <summary>
        /// The message containing details about the failed product fetch.
        /// </summary>
        public string message;
        /// <summary>
        /// Specifies if the request can be retried.
        /// </summary>
        public bool isRetryable;

        /// <summary>
        /// The reason for the failure.
        /// </summary>
        public ProductFetchFailureReason Reason => reason;

        /// <summary>
        /// The message containing details about the failed product fetch.
        /// </summary>
        public string Message => message;

        /// <summary>
        /// Specifies if the request is retryable.
        /// </summary>
        public bool IsRetryable => isRetryable;

        /// <summary>
        /// Parametrized Constructor.
        /// </summary>
        /// <param name="reason"> The reason for the product fetch failure </param>
        /// <param name="message"> The message containing details about the product fetch failure. </param>
        /// <param name="isRetryable"> Specifies if the query can be retried. </param>
        public ProductFetchFailureDescription(ProductFetchFailureReason reason, string message,
            bool isRetryable = false)
        {
            this.reason = reason;
            this.message = message;
            this.isRetryable = isRetryable;
        }
    }
}
