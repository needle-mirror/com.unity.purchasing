#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class AggregateRetryPolicy : IRetryPolicy
    {
        private readonly List<IRetryPolicy> m_RetryPolicies;

        public AggregateRetryPolicy(List<IRetryPolicy> retryPolicies)
        {
            m_RetryPolicies = retryPolicies;
        }

        public AggregateRetryPolicy(params IRetryPolicy[] retryPolicies)
        {
            m_RetryPolicies = new List<IRetryPolicy>(retryPolicies);
        }

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
