using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for a class that handles callbacks for confirming purchases made.
    /// </summary>
    public interface IStorePurchaseConfirmCallback
    {
        /// <summary>
        /// Inform Unity Purchasing of a confirmed order.
        /// </summary>
        /// <param name="transactionId"> The transaction id of the confirmed order </param>
        void OnConfirmOrderSucceeded(string transactionId);

        /// <summary>
        /// Notify a failed order confirmation with associated details.
        /// </summary>
        /// <param name="failedOrder"> The object detailing the purchase failure </param>
        void OnConfirmOrderFailed(FailedOrder failedOrder);
    }
}
