#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception thrown when an attempt to purchase a product encounters a problem.
    /// </summary>
    [Serializable]
    public class PurchaseException : IapException
    {
        /// <summary>
        /// Construct an error object for making a purchase.
        /// </summary>
        public PurchaseException() { }

        /// <summary>
        /// Construct an error object for making a purchase.
        /// </summary>
        /// <param name="message">Description of error</param>
        public PurchaseException(string message) : base(message) { }

        protected PurchaseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
