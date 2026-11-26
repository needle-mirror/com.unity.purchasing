#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception associated with the IAP package.
    /// </summary>
    [Serializable]
    public abstract class IapException : Exception
    {
        internal IapException() { }

        internal IapException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IapException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        // .NET 8 obsoletes formatter-based serialization (SYSLIB0051). Removing this constructor is an API break.
#pragma warning disable SYSLIB0051
        protected IapException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore SYSLIB0051
    }
}
