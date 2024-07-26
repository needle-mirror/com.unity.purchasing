#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Error found performing a fetch of products from a store.
    /// </summary>
    [Serializable]
    public class ProductFetchException : IapException
    {
        /// <summary>
        /// Construct an error object for product fetching.
        /// </summary>
        public ProductFetchException() { }

        /// <summary>
        /// Construct an error object for product fetching.
        /// </summary>
        /// <param name="message">Description of the error.</param>
        public ProductFetchException(string message) : base(message) { }

        protected ProductFetchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
