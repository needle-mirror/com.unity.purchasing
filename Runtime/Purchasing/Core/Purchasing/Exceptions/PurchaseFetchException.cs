#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception thrown when an fetching purchases encounters a problem.
    /// </summary>
    [Serializable]
    public class PurchaseFetchException : IapException
    {
        /// <summary>
        /// Construct an error object for fetching purchases.
        /// </summary>
        public PurchaseFetchException() { }

        /// <summary>
        /// Construct an error object for fetching purchases.
        /// </summary>
        /// <param name="message">Description of error</param>
        public PurchaseFetchException(string message) : base(message) { }

        protected PurchaseFetchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
