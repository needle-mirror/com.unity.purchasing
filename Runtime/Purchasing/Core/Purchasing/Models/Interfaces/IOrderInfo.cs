using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about an order.
    /// </summary>
    public interface IOrderInfo
    {
        /// <summary>
        /// Apple specific OrderInfo class
        /// </summary>
        IAppleOrderInfo Apple { get; }

        /// <summary>
        /// Additional information for purchased products found in a `ConfirmedOrder`.
        /// </summary>
        List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }

        /// <summary>
        /// The purchase receipt, in JSON format. Read only.
        ///
        /// Consumable's `Receipt` are not set between app restarts unless it is a pending order.
        /// Once a consumable has been acknowledged (ConfirmOrder) the `Receipt` is removed.
        ///
        /// The receipt provided while on the Apple App Store will be the full receipt of every purchase so far.
        /// </summary>
        string Receipt { get; }

        /// <summary>
        /// The transaction ID of the purchase. Read only.
        ///
        /// Consumable's transactionID are not set between app restarts unless it is a pending order.
        /// Once a consumable has been acknowledged (ConfirmOrder) the `transactionID` is removed.
        /// </summary>
        string TransactionID { get; }
    }
}
