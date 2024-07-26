#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  A model representing the failure to fetch preexisting purchases.
    /// </summary>
    [Serializable]
    public class PurchasesFetchFailureDescription
    {
        public PurchasesFetchFailureReason failureReason;
        public string message;

        /// <summary>
        /// The reason the fetch failed. Read only.
        /// </summary>
        public PurchasesFetchFailureReason FailureReason => failureReason;

        /// <summary>
        /// The message describing why fetch failed. Read only.
        /// </summary>
        public string Message => message;

        /// <summary>
        ///  Constructs the fetch failed object
        /// </summary>
        /// <param name="reason">The reason the fetch failed.</param>
        /// <param name="message">The message describing why the fetch failed.</param>
        public PurchasesFetchFailureDescription(PurchasesFetchFailureReason reason, string message)
        {
            failureReason = reason;
            this.message = message;
        }
    }
}
