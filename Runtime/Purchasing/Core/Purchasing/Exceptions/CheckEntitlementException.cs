#nullable enable

using System;
using System.Runtime.Serialization;

namespace UnityEngine.Purchasing
{
    [Serializable]
    public class CheckEntitlementException : IapException
    {
        /// <summary>
        /// Construct an error object for checking if a product is entitled.
        /// </summary>
        public CheckEntitlementException() { }

        /// <summary>
        /// Construct an error object for checking if a product is entitled.
        /// </summary>
        /// <param name="message">Description of error</param>
        public CheckEntitlementException(string message) : base(message) { }

        protected CheckEntitlementException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
