using System.Threading.Tasks;
using Purchasing.TransactionVerifier;

namespace UnityEngine.Purchasing.TransactionVerifier
{
    /// <summary>
    /// Interface for verifying transactions.
    /// </summary>
    public interface ITransactionVerifier
    {
        /// <summary>
        /// Verifies a pending order.
        /// </summary>
        /// <param name="transactionRepresentation">The transaction representation.</param>
        /// <returns>Returns a task that resolves to a VerificationResponse.</returns>
        Task<VerificationResponse> VerifyPendingOrder(string transactionRepresentation);
    }
}
