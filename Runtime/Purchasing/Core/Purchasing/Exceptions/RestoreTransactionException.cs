using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    public class RestoreTransactionException : IapException
    {
        /// <summary>
        /// Construct an error object for restoring a purchase.
        /// </summary>
        public RestoreTransactionException() { }

        /// <summary>
        /// Construct an error object for restoring a purchase.
        /// </summary>
        /// <param name="message">Description of error</param>
        public RestoreTransactionException(string message) : base(message) { }

        protected RestoreTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
