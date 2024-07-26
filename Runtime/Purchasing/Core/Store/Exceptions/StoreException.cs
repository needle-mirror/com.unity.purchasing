#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Exception thrown when an attempt to get a store encounters a problem.
    /// </summary>
    public class StoreException : Exception
    {
        /// <summary>
        /// Construct an error object for getting a store.
        /// </summary>
        public StoreException() { }

        /// <summary>
        /// Construct an error object for getting a store.
        /// </summary>
        /// <param name="message">Description of error</param>
        public StoreException(string message) : base(message) { }

        /// <summary>
        /// Construct an error object for getting a store.
        /// </summary>
        /// <param name="message">Description of error</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public StoreException(string message, Exception innerException) : base(message, innerException) { }
    }
}
