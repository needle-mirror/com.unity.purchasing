//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace UnityEngine.Purchasing.TransactionVerifier.Http
{
    /// <summary>
    /// HTTP wrapper interface.
    /// </summary>
    internal interface IHttpClient
    {
        /// <summary>Performs an asynchronous HTTP request.</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="url">The HTTP request URL.</param>
        /// <param name="body">Byte array representing the request body.</param>
        /// <param name="headers">Dictionary of headers for the request.</param>
        /// <param name="requestTimeout">Request timeout value.</param>
        /// <returns> </returns>
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, byte[] body, Dictionary<string, string> headers, int requestTimeout);

        /// <summary>Performs an asynchronous Http request for multipart uploads</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="url">The HTTP request URL.</param>
        /// <param name="body">Byte array representing the request body.</param>
        /// <param name="headers">Dictionary of headers for the request.</param>
        /// <param name="requestTimeout">Request timeout value.</param>
        /// <param name="boundary">The string delimiter for each multipart section.</param>
        /// <returns> </returns>
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, List<IMultipartFormSection> body, Dictionary<string, string> headers, int requestTimeout, string boundary = null);
    }
}
