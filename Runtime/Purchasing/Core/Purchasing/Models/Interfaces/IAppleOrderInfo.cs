#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about an Apple order.
    /// </summary>
    public interface IAppleOrderInfo
    {
        /// <summary>
        /// Read the latest App Receipt.
        /// Returns null for iOS less than or equal to 6, may also be null on a reinstalling and require refreshing.
        /// </summary>
        string? AppReceipt { get; }

        /// <summary>
        /// The original transaction ID of the purchase.
        /// </summary>
        string? OriginalTransactionID { get; set; }

        /// <summary>
        /// The ownership type of the purchase.
        /// </summary>
        OwnershipType OwnershipType { get; set; }

        /// <summary>
        /// Indicates name of the store.
        /// </summary>
        string StoreName { get; set; }

        /// <summary>
        /// The app-specific account token associated with the purchase.
        /// This is used to link transactions to a specific app account for additional validation.
        /// </summary>
        Guid? AppAccountToken { get; set; }

        /// <summary>
        /// The JWS representation containing the transaction information.
        /// https://developer.apple.com/documentation/storekit/verificationresult/jwsrepresentation-21vgo
        /// </summary>
        string? jwsRepresentation { get; set; }
    }
}
