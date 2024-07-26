#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception thrown when an attempt to confirm a purchase encounters a problem.
    /// </summary>
    [Serializable]
    public class ConfirmOrderException : IapException
    {
        /// <summary>
        /// Construct an error object for confirming a purchase.
        /// </summary>
        public ConfirmOrderException() { }

        /// <summary>
        /// Construct an error object for confirming a purchase.
        /// </summary>
        /// <param name="message">Description of error</param>
        public ConfirmOrderException(string message) : base(message) { }

        protected ConfirmOrderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
