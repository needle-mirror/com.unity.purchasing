#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    interface IRetryRequest
    {
        /// <summary>
        /// Invoke the request if the <see cref="IRetryPolicy"/> permits it.
        /// </summary>
        /// <returns>true if the request was retried, false otherwise</returns>
        public Task<bool> Invoke();

        /// <summary>
        /// Invoke the request if the <see cref="IRetryPolicy"/> and the <see cref="IRetryableRequestFailureDescription"/> permits it.
        /// </summary>
        /// <returns>true if the request was retried, false otherwise</returns>
        public Task<bool> Retry(IRetryableRequestFailureDescription requestFailureDescription);
    }
}
