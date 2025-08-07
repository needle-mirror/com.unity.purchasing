#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A retry policy that limits the number of attempts to a specified maximum.
    /// </summary>
    public class MaximumNumberOfAttemptsRetryPolicy : IRetryPolicy
    {
        readonly int m_MaximumNumberOfAttempts;

        /// <summary>
        /// Initializes a maximum number of attempts retry policy with the specified maximum number of attempts.
        /// </summary>
        /// <param name="maximumNumberOfAttempts">The maximum number of attempts allowed before giving up.</param>
        public MaximumNumberOfAttemptsRetryPolicy(int maximumNumberOfAttempts)
        {
            m_MaximumNumberOfAttempts = maximumNumberOfAttempts;
        }

        /// <summary>
        /// Determines whether the retry policy should allow another attempt based on the number of attempts made.
        /// </summary>
        /// <param name="info">The information about the retry attempt, including the number of attempts made.</param>
        /// <returns>True if another attempt should be made; otherwise, false.</returns>
        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(info.NumberOfAttempts <= m_MaximumNumberOfAttempts);
        }
    }
}
