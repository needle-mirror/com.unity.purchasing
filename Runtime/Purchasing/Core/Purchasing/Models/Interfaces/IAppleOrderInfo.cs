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
        string AppReceipt { get; }

        /// <summary>
        /// The original transaction ID of the purchase.
        /// </summary>
        string OriginalTransactionID { get; set; }

        /// <summary>
        /// Indicates whether the purchase is restored.
        /// </summary>
        bool IsRestored { get; set; }
    }
}
