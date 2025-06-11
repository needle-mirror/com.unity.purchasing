#nullable enable

namespace UnityEngine.Purchasing.Exceptions
{
    class FetchProductsException : IapException
    {
        internal ProductFetchFailureDescription FailureDescription { get; }

        public FetchProductsException(ProductFetchFailureDescription failureDescription) : base(failureDescription.Message)
        {
            FailureDescription = failureDescription;
        }
    }
}
