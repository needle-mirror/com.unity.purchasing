#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception thrown when an attempt to create a service encounters a problem.
    /// </summary>
    public class ServiceCreationException : IapException
    {
        internal ServiceCreationException() { }

        internal ServiceCreationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the ServiceCreationException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ServiceCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
