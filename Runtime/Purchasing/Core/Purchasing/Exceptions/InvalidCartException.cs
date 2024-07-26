#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception associated with a cart item.
    /// </summary>
    [Serializable]
    public class InvalidCartException : IapException
    {
        /// <summary>
        /// Create a default InvalidCartException.
        /// </summary>
        public InvalidCartException() { }

        /// <summary>
        /// Create an InvalidCartException with a message string.
        /// </summary>
        /// <param name="message"> The message describing the exception. </param>
        public InvalidCartException(string message) : base(message) { }

        /// <summary>
        /// Create a serialized InvalidCartException with SerializationInfo and a StreamingContext.
        /// </summary>
        /// <param name="info"> The <c>SerializationInfo</c> used to serialize this constructor. </param>
        /// <param name="context"> The <c>StreamingContext</c> used to serialize this constructor. </param>
        protected InvalidCartException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
