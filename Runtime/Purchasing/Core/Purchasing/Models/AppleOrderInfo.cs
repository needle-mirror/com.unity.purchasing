#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    class AppleOrderInfo : OrderInfo, IAppleOrderInfo
    {
        IAppleAppReceiptViewer m_ReceiptViewer;
        public string? AppReceipt => m_ReceiptViewer.AppReceipt();
        public string? OriginalTransactionID { get; set; }
        public OwnershipType OwnershipType { get; set; }
        public string StoreName { get; set; }
        public Guid? AppAccountToken { get; set; }
        public string? jwsRepresentation { get; set; }

        public AppleOrderInfo(string transactionID, string storeName, IAppleAppReceiptViewer appReceiptViewer, string? originalTransactionID, OwnershipType ownershipType, Guid? appAccountToken, string? signatureJws)
            : base(string.Empty, transactionID, storeName)
        {
            m_ReceiptViewer = appReceiptViewer;
            OriginalTransactionID = originalTransactionID;
            StoreName = storeName ?? string.Empty;
            OwnershipType = ownershipType;
            AppAccountToken = appAccountToken;
            jwsRepresentation = signatureJws;
        }
    }
}
