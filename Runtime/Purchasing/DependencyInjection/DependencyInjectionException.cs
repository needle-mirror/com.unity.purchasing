#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    [Serializable]
    class DependencyInjectionException : Exception
    {
        internal DependencyInjectionException() { }

        internal DependencyInjectionException(string message) : base(message) { }

        protected DependencyInjectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
