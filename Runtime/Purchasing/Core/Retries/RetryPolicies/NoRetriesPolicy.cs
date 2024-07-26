using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class NoRetriesPolicy : IRetryPolicy
    {
        public virtual Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            return Task.FromResult(false);
        }
    }
}
