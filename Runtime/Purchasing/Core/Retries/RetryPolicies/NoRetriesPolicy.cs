using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A retry policy that does not allow any retries.
    /// </summary>
    public class NoRetriesPolicy : IRetryPolicy
    {
        /// <summary>
        /// Determines whether the retry policy should allow another attempt.
        /// </summary>
        /// <param name="info">The information about the retry attempt.</param>
        /// <returns>Always returns false.</returns>
        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(false);
        }
    }
}
