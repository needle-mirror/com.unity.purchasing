#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A retry policy that retries operations until a specified time limit is reached.
    /// This policy will continue retrying failed operations as long as the total elapsed time
    /// is less than the configured time limit.
    /// </summary>
    public class TimeLimitRetryPolicy : IRetryPolicy
    {
        readonly float m_TimeLimit;

        /// <summary>
        /// Initializes a new instance of the TimeLimitRetryPolicy class with the specified time limit.
        /// </summary>
        /// <param name="timeLimit">The maximum time in seconds to continue retrying operations before giving up.</param>
        public TimeLimitRetryPolicy(float timeLimit)
        {
            m_TimeLimit = timeLimit;
        }

        /// <summary>
        /// Determines whether a failed operation should be retried based on the elapsed time.
        /// </summary>
        /// <param name="info">Information about the current retry attempt, including elapsed time and attempt count.</param>
        /// <returns>
        /// A task that resolves to <c>true</c> if the operation should be retried (elapsed time is less than the time limit),
        /// or <c>false</c> if the time limit has been exceeded and no further retries should be attempted.
        /// </returns>
        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(info.TimeSinceFirstAttempt <= m_TimeLimit);
        }
    }
}
