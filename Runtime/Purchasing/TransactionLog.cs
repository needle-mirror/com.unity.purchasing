using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Records processed transactions on the file system
    /// for de duplication purposes.
    /// </summary>
    internal class TransactionLog
    {
        private readonly ILogger logger;
        private readonly string persistentDataPath;
        private HashSet<string> recordCache;

        public TransactionLog(ILogger logger, string persistentDataPath)
        {
            this.logger = logger;
            if (!string.IsNullOrEmpty(persistentDataPath))
            {
                this.persistentDataPath = Path.Combine(Path.Combine(persistentDataPath, "Unity"), "UnityPurchasing");
            }
            recordCache = new HashSet<string>();
        }

        /// <summary>
        /// Removes all transactions from the log.
        /// </summary>
        public void Clear()
        {
            Directory.Delete(persistentDataPath, true);
            recordCache.Clear();
        }

        public bool HasRecordOf(string transactionID)
        {
            if (string.IsNullOrEmpty(transactionID) || string.IsNullOrEmpty(persistentDataPath))
            {
                return false;
            }
            else if (recordCache.Contains(transactionID))
            {
                return true;
            }

            bool exists = Directory.Exists(GetRecordPath(transactionID));
            if (exists)
            {
                recordCache.Add(transactionID);
            }
            return exists;
        }

        public void Record(string transactionID)
        {
            // Consumables have additional de-duplication logic.
            if (!(string.IsNullOrEmpty(transactionID) || string.IsNullOrEmpty(persistentDataPath)))
            {
                var path = GetRecordPath(transactionID);
                try
                {
                    Directory.CreateDirectory(path);
                    recordCache.Add(transactionID);
                }
                catch (Exception recordPathException)
                {
                    // A wide variety of exceptions can occur, for all of which
                    // nothing is the best course of action.
                    logger.LogException(recordPathException);
                }
            }
        }

        private string GetRecordPath(string transactionID)
        {
            return Path.Combine(persistentDataPath, ComputeHash(transactionID));
        }

        /// <summary>
        /// Compute a 64 bit Knuth hash of a transaction ID.
        /// This should be more than sufficient for the few thousand maximum
        /// products expected in an App.
        /// </summary>
        internal static string ComputeHash(string transactionID)
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
