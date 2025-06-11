#nullable enable

using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Exceptions
{
    class GoogleFetchProductException : FetchProductsException
    {
        GoogleFetchProductsFailureReason FailureReason { get; }
        GoogleBillingResponseCode ResponseCode { get; }

        public GoogleFetchProductException(GoogleFetchProductsFailureReason failureReason, GoogleBillingResponseCode responseCode,
            ProductFetchFailureDescription failureDescription) : base(failureDescription)
        {
            FailureReason = failureReason;
            ResponseCode = responseCode;
        }
    }
}
