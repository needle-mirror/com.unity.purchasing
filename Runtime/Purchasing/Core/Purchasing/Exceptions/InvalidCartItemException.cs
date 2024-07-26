#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception associated with a cart item.
    /// </summary>
    [Serializable]
    public class InvalidCartItemException : IapException
    {
        /// <summary>
        /// Create a default InvalidCartItemException.
        /// </summary>
        public InvalidCartItemException() { }

        /// <summary>
        /// Create an InvalidCartItemException with a message string.
        /// </summary>
        /// <param name="message"> The message describing the exception. </param>
        public InvalidCartItemException(string message) : base(message) { }

        /// <summary>
        /// Create a serialized InvalidCartItemException with SerializationInfo and a StreamingContext.
        /// </summary>
        /// <param name="info"> The <c>SerializationInfo</c> used to serialize this constructor. </param>
        /// <param name="context"> The <c>StreamingContext</c> used to serialize this constructor. </param>
        protected InvalidCartItemException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
