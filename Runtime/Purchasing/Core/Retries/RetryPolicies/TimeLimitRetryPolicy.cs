#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class TimeLimitRetryPolicy : IRetryPolicy
    {
        readonly float m_TimeLimit;

        public TimeLimitRetryPolicy(float timeLimit)
        {
            m_TimeLimit = timeLimit;
        }
        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(info.TimeSinceFirstAttempt <= m_TimeLimit);
        }
    }
}
