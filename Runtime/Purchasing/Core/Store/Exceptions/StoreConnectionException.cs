#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception associated store connection.
    /// </summary>
    [Serializable]
    public class StoreConnectionException : IapException
    {
        internal StoreConnectionException(string message) : base(message) { }

        internal StoreConnectionException(StoreConnectionFailureDescription failureDescription) : base(failureDescription.Message) { }

        protected StoreConnectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
