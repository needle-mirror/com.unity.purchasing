#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for retry policies used in purchase operations.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Determines if a retry should be attempted based on the provided information.
        /// </summary>
        /// <param name="info">The retry policy information.</param>
        /// <returns>A task that resolves to a boolean indicating whether a retry should be attempted.</returns>
        Task<bool> ShouldRetry(IRetryPolicyInformation info);
    }
}
