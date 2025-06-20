//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;
using UnityEngine.Scripting;
using UnityEngine.Purchasing.TransactionVerifier;

namespace UnityEngine.Purchasing.TransactionVerifier.Http
{
    /// <summary>
    /// An HttpException which is thrown if a network or Http error occurs.
    /// </summary>
    [Serializable]
    [Preserve]
    internal class HttpException : Exception
    {
        /// <summary>
        /// Instance of the HttpClientResponse. Can be used for handling the exception.
        /// </summary>
        [Preserve]
        public HttpClientResponse Response;

        ///<inheritdoc cref="Exception"/>
        [Preserve]
        public HttpException() : base()
        {
        }

        ///<inheritdoc cref="Exception"/>
        [Preserve]
        public HttpException(string message) : base(message)
        {
        }

        ///<inheritdoc cref="Exception"/>
        [Preserve]
        public HttpException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>HttpException Constructor</summary>
        /// <param name="response">The HttpClientResponse that triggered the exception.</param>
        [Preserve]
        public HttpException(HttpClientResponse response) : base($"({response.StatusCode}) {response.ErrorMessage}")
        {
            Response = response;
        }
    }

    /// <summary>
    /// An HttpException where we have deserialized the response body and
    /// identified the specific error  and can provide more specific information.
    /// </summary>
    /// <typeparam name="T">The type of the error triggered.</typeparam>
    [Serializable]
    [Preserve]
    internal class HttpException<T> : HttpException
    {
        /// <summary>Instance of the actual error object triggered.</summary>
        [Preserve]
        public T ActualError;

        ///<inheritdoc cref="HttpException"/>
        [Preserve]
        public HttpException() : base()
        {
        }

        ///<inheritdoc cref="HttpException"/>
        [Preserve]
        public HttpException(string message) : base(message)
        {
        }

        ///<inheritdoc cref="HttpException"/>
        [Preserve]
        public HttpException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>HttpException Constructor.</summary>
        /// <param name="response">The response that contained the error.</param>
        /// <param name="actualError">The deserialized error object.</param>
        [Preserve]
        public HttpException(HttpClientResponse response, T actualError) : base(response)
        {
            ActualError = actualError;
        }
    }
}
