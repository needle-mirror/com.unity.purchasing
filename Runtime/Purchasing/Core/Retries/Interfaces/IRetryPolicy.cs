#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public interface IRetryPolicy
    {
        Task<bool> ShouldRetry(IRetryPolicyInformation info);
    }
}
