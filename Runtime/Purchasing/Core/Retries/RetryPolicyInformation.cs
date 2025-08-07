#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Contains information about retry attempts for purchase operations.
    /// Provides data about the number of retry attempts and timing information for retry policy decisions.
    /// </summary>
    public struct RetryPolicyInformation : IRetryPolicyInformation
    {
        /// <summary>
        /// Gets the total number of attempts made for the current operation.
        /// Includes the initial attempt plus all retry attempts.
        /// </summary>
        public int NumberOfAttempts { get; }

        /// <summary>
        /// Gets the time elapsed since the first attempt was made, in seconds.
        /// Used to determine if retry timeouts have been exceeded.
        /// </summary>
        public float TimeSinceFirstAttempt { get; }

        internal RetryPolicyInformation(int numberOfAttempts, float timeSinceFirstAttempt)
        {
            NumberOfAttempts = numberOfAttempts;
            TimeSinceFirstAttempt = timeSinceFirstAttempt;
        }
    }
}
