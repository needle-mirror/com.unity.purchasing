#nullable enable

namespace UnityEngine.Purchasing.Exceptions
{
    internal class GoogleFinishTransactionException : IapException
    {
        internal PurchaseFailureReason FailureReason { get; }

        internal GoogleFinishTransactionException(PurchaseFailureReason failureReason, string message) : base(message)
        {
            FailureReason = failureReason;
        }
    }
}
