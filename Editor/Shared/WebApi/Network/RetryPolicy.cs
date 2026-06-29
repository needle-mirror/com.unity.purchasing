using System;
using System.Threading.Tasks;

namespace Unity.Purchasing.Editor.Shared.WebApi.Network
{
    class RetryPolicy : IRetryPolicy
    {
        const int k_DefaultRetries = 2;

        public Func<ApiResponse, int, Task<bool>> Policy { get; set; }

        public RetryPolicy()
        {
            Policy = ShouldRetryAsync;
        }

        Task<bool> ShouldRetryAsync(ApiResponse response, int attempt)
        {
            return Task.FromResult(attempt < k_DefaultRetries && response.ErrorType == ApiErrorType.Network);
        }
    }
}
