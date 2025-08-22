#nullable enable

using System;
using System.IO;
using System.Text;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Records processed transactions on the file system
    /// for de duplication purposes.
    /// </summary>
    class TransactionLog : ITransactionLog
    {
        readonly string? m_PersistentDataPath;

        public TransactionLog(string persistentDataPath)
        {
            if (!string.IsNullOrEmpty(persistentDataPath))
            {
                m_PersistentDataPath = Path.Combine(Path.Combine(persistentDataPath, "Unity"), "UnityPurchasing");
            }
        }

        public void Clear()
        {
            if (!string.IsNullOrEmpty(m_PersistentDataPath))
            {
                Directory.Delete(m_PersistentDataPath, true);
            }
        }

        public bool HasRecordOf(string transactionID)
        {
            if (string.IsNullOrEmpty(transactionID) || string.IsNullOrEmpty(m_PersistentDataPath))
            {
                return false;
            }

            return Directory.Exists(GetRecordPath(m_PersistentDataPath!, transactionID));
        }

        public void Record(string transactionID)
        {
            // Consumables have additional de-duplication logic.
            if (string.IsNullOrEmpty(transactionID) || string.IsNullOrEmpty(m_PersistentDataPath))
            {
                return;
            }

            var path = GetRecordPath(m_PersistentDataPath!, transactionID);
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception recordPathException)
            {
                // A wide variety of exceptions can occur, for all of which
                // nothing is the best course of action.
                Debug.unityLogger.LogException(recordPathException);
            }

        }

        static string GetRecordPath(string dataPath, string transactionID)
        {
            return Path.Combine(dataPath, ComputeHash(transactionID));
        }

        /// <summary>
        /// Compute a 64 bit Knuth hash of a transaction ID.
        /// This should be more than sufficient for the few thousand maximum
        /// products expected in an App.
        /// </summary>
        static string ComputeHash(string transactionID)
        {
            var hash = 3074457345618258791ul;
            for (var i = 0; i < transactionID.Length; i++)
            {
                hash += transactionID[i];
                hash *= 3074457345618258799ul;
            }

            var builder = new StringBuilder(16);
            foreach (var b in BitConverter.GetBytes(hash))
            {
                builder.AppendFormat("{0:X2}", b);
            }
            return builder.ToString();
        }
    }
}
