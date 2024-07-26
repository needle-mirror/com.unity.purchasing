
namespace UnityEngine.Purchasing
{
    class TransactionFailedEventHelper
    {
        internal static string BuildFailureReason(PurchaseFailureDescription failureDescription)
        {
            var failureReason = $"Failure reason: {failureDescription.reason.ToString()}";
            if (!string.IsNullOrEmpty(failureDescription.message))
            {
                failureReason += $" Failure message: {failureDescription.message}";
            }

            return failureReason;
        }
    }
}
