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

        protected ServiceCreationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
