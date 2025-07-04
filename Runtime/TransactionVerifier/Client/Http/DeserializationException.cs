//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;

namespace UnityEngine.Purchasing.TransactionVerifier.Http
{
    /// <summary>
    /// DeserializationException class.
    /// </summary>
    [Serializable]
    internal class DeserializationException : Exception
    {
        /// <summary>Default Constructor.</summary>
        public DeserializationException() : base()
        {
        }

        /// <summary>Constructor.</summary>
        /// <param name="message">Custom error message</param>
        public DeserializationException(string message) : base(message)
        {
        }

        /// <summary>DeserializationException with message and inner exception.</summary>
        /// <param name="message">Custom error message.</param>
        /// <param name="inner">Inner exception.</param>
        DeserializationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
