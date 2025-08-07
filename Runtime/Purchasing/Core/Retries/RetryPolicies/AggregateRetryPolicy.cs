#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  An aggregate retry policy that combines multiple retry policies into one.
    /// </summary>
    public class AggregateRetryPolicy : IRetryPolicy
    {
        readonly List<IRetryPolicy> m_RetryPolicies;

        /// <summary>
        /// Constructs an AggregateRetryPolicy with a list of retry policies.
        /// </summary>
        /// <param name="retryPolicies">The list of retry policies to aggregate.</param>
        public AggregateRetryPolicy(List<IRetryPolicy> retryPolicies)
        {
            m_RetryPolicies = retryPolicies;
        }

        /// <summary>
        /// Constructs an AggregateRetryPolicy with a variable number of retry policies.
        /// </summary>
        /// <param name="retryPolicies">The retry policies to aggregate.</param>
        public AggregateRetryPolicy(params IRetryPolicy[] retryPolicies)
        {
            m_RetryPolicies = new List<IRetryPolicy>(retryPolicies);
        }

        /// <summary>
        /// Determines whether the operation should be retried based on the provided retry policy information.
        /// </summary>
        /// <param name="info"> The information about the retry policy.</param>
        /// <returns>True if the operation should be retried, otherwise false.</returns>
        public async Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            foreach (var retryPolicy in m_RetryPolicies)
            {
                if (!await retryPolicy.ShouldRetry(info))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
