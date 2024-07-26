#nullable enable

using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Exceptions
{
    class GoogleRetrieveProductException : RetrieveProductsException
    {
        GoogleRetrieveProductsFailureReason FailureReason { get; }
        GoogleBillingResponseCode ResponseCode { get; }

        public GoogleRetrieveProductException(GoogleRetrieveProductsFailureReason failureReason, GoogleBillingResponseCode responseCode,
            ProductFetchFailureDescription failureDescription) : base(failureDescription)
        {
            FailureReason = failureReason;
            ResponseCode = responseCode;
        }
    }
}
