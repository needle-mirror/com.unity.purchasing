#nullable enable
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
        IAppleOrderInfo? Apple { get; }

        /// <summary>
        /// Google specific OrderInfo class
        /// </summary>
        IGoogleOrderInfo? Google { get; }

        /// <summary>
        /// Additional information for purchased products found in a `ConfirmedOrder`.
        /// </summary>
        List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }

        /// <summary>
        /// The receipt, in JSON format. Read only.
        ///
        /// For an order containing only consumable products, the `Receipt` will be only be available if it is
        /// a `PendingOrder`.
        /// Once it has been confirmed (`ConfirmedOrder`), the `Receipt` is empty as it is no longer returned
        /// from the store.
        ///
        /// The receipt provided while on the Apple App Store will be the full receipt of every purchase so far.
        /// </summary>
        string Receipt { get; }

        /// <summary>
        /// The transaction ID of the purchase. Read only.
        ///
        /// Consumable's transactionID are not set between app restarts unless it is a `PendingOrder`.
        /// Once it has been confirmed (`ConfirmedOrder`), the `TransactionID` is empty as it is no longer returned
        /// from the store.
        /// </summary>
        string TransactionID { get; }
    }
}
