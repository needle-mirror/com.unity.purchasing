#nullable enable
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about a Payment Providers order.
    /// </summary>
    public interface IPaymentProvidersOrderInfo
    {
        /// <summary>
        /// Optional. A unique custom identifier that you can set to any value to help reconcile IAP Orders
        /// with your own internal system.
        /// </summary>
        public string? CustomReferenceId { get; }
        /// <summary>
        /// Optional. Arbitrary key/value metadata stored with the order
        /// and returned in the order response and order webhooks.
        /// </summary>
        public IReadOnlyDictionary<string, string>? Metadata { get; }
    }
}
