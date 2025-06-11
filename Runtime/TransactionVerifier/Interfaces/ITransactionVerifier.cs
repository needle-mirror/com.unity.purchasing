using System.Threading.Tasks;
using Purchasing.TransactionVerifier;

namespace UnityEngine.Purchasing.TransactionVerifier
{
    public interface ITransactionVerifier
    {
        Task<VerificationResponse> VerifyPendingOrder(string transactionRepresentation);
    }
}
