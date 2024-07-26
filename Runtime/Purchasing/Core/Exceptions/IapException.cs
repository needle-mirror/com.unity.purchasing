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

        protected IapException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
