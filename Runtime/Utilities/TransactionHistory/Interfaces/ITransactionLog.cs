#nullable enable

namespace UnityEngine.Purchasing
{
    interface ITransactionLog
    {
        void Clear();
        bool HasRecordOf(string transactionID);
        void Record(string transactionID);
    }
}
