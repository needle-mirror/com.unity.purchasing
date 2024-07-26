#nullable enable

namespace UnityEngine.Purchasing.Exceptions
{
    class RetrieveProductsException : IapException
    {
        internal ProductFetchFailureDescription FailureDescription { get; }

        public RetrieveProductsException(ProductFetchFailureDescription failureDescription) : base(failureDescription.Message)
        {
            FailureDescription = failureDescription;
        }
    }
}
