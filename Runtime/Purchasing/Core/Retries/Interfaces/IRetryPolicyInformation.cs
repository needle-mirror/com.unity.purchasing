#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for providing information about retry policy attempts.
    /// </summary>
    public interface IRetryPolicyInformation
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
    }
}
