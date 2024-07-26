#nullable enable
namespace UnityEngine.Purchasing
{
    class AppleOrderInfo : OrderInfo, IAppleOrderInfo
    {
        IAppleAppReceiptViewer m_ReceiptViewer;

        /// <summary>
        /// Read the latest App Receipt.
        /// Returns null for iOS less than or equal to 6, may also be null on a reinstalling and require refreshing.
        /// </summary>
        public string? AppReceipt => m_ReceiptViewer.appReceipt;

        /// <summary>
        /// The original transaction ID of the purchase.
        /// </summary>
        public string? OriginalTransactionID { get; set; }

        /// <summary>
        /// Indicates whether the purchase is restored.
        /// </summary>
        public bool IsRestored { get; set; }

        public AppleOrderInfo(string transactionID, string? storeName, IAppleAppReceiptViewer appReceiptViewer, string? originalTransactionID = null, bool isRestored = false)
            : base(string.Empty, transactionID, storeName)
        {
            m_ReceiptViewer = appReceiptViewer;
            IsRestored = isRestored;
            OriginalTransactionID = originalTransactionID;
        }
    }
}
