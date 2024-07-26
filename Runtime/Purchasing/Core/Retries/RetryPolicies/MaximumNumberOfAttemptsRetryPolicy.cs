#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class MaximumNumberOfAttemptsRetryPolicy : IRetryPolicy
    {
        readonly int m_MaximumNumberOfAttempts;

        public MaximumNumberOfAttemptsRetryPolicy(int maximumNumberOfAttempts)
        {
            m_MaximumNumberOfAttempts = maximumNumberOfAttempts;
        }

        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(info.NumberOfAttempts <= m_MaximumNumberOfAttempts);
        }
    }
}
