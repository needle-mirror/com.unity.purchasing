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
        /// On Google Play, it includes Google's debug message and billing response code.
        /// </summary>
        public string message;

        /// <summary>
        /// Whether calling <c>Connect()</c> again could succeed. See <see cref="IsRetryable"/>.
        /// </summary>
        public bool isRetryable;

        /// <summary>
        /// The message containing details about the failed connection attempt.
        /// On Google Play, it includes Google's debug message and billing response code.
        /// </summary>
        public string Message => message;

        /// <summary>
        /// Whether calling <c>Connect()</c> again could succeed.
        /// </summary>
        /// <remarks>
        /// IAP may already have retried the connection before reporting the failure, so true doesn't mean you should
        /// retry at once. Some failures only clear after the user acts, for example Google Play's
        /// <c>BillingUnavailable</c> until the user updates the Play Store or signs in. Offer a retry the user starts,
        /// such as a "Try again" button, rather than retrying in a loop. False means the connection can't succeed
        /// without a change to the app.
        /// </remarks>
        public bool IsRetryable => isRetryable;

        // Whether IAP reconnects on its own before reporting the failure. Kept apart from IsRetryable, which tells the
        // developer whether a retry of their own can succeed: some failures only clear after the user acts.
        [NonSerialized]
        bool? m_ShouldAutoRetry;

        internal bool ShouldAutoRetry => m_ShouldAutoRetry ?? isRetryable;

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

        internal StoreConnectionFailureDescription(string message, bool isRetryable, bool shouldAutoRetry)
            : this(message, isRetryable)
        {
            m_ShouldAutoRetry = shouldAutoRetry;
        }
    }
}
