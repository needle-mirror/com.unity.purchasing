//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.TransactionVerifier.Http
{
    /// <summary>
    /// IDeserializable is an interface for wrapping generic objects that might
    /// be returned as part of HTTP requests.
    /// </summary>
    [Preserve]
    [JsonConverter(typeof(JsonObjectConverter))]
    internal interface IDeserializable
    {
        /// <summary>
        /// Returns the internal object as a string.
        /// </summary>
        /// <returns>The internal object as a string.</returns>
        string GetAsString();

        /// <summary>
        /// Gets this object as the given type.
        /// </summary>
        /// <typeparam name="T">The type you want to convert this object to.</typeparam>
        /// <param name="deserializationSettings">Deserialization settings for how to handle properties like missing members.</param>
        /// <returns>This object as the given type.</returns>
        T GetAs<T>(DeserializationSettings deserializationSettings = null);
    }

}
