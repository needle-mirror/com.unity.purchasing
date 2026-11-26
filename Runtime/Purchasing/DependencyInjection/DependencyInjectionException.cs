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

        // .NET 8 obsoletes formatter-based serialization (SYSLIB0051).
#pragma warning disable SYSLIB0051
        protected DependencyInjectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore SYSLIB0051
    }
}
