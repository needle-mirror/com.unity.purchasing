#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about an order.
    /// </summary>
    /// <remarks>
    /// The OrderInfo fields can be empty for a DeferredOrder or FailedOrder since these fields are associated to a
    /// paid purchase.
    /// </remarks>
    class OrderInfo : IOrderInfo
    {
        public IAppleOrderInfo? Apple => this as IAppleOrderInfo;
        public IGoogleOrderInfo? Google => this as IGoogleOrderInfo;

        public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }

        public string TransactionID { get; }

        public string Receipt
        {
            get => GetReceipt();
            private set => SetReceipt(value);
        }

        string GetReceipt()
        {
            if (Apple != null)
            {
                return CreateUnifiedReceipt(Apple.AppReceipt ?? "", TransactionID, Apple.StoreName);
            }
            return m_Receipt;
        }

        void SetReceipt(string receipt)
        {
            if (Apple == null)
            {
                m_Receipt = receipt;
            }
        }

        string m_Receipt = "";

        /// <summary>
        /// Creates a new OrderInfo instance.
        /// </summary>
        /// <param name="receipt">The receipt for the order. May be empty for failed orders.</param>
        /// <param name="transactionID">The transaction ID for the order. May be empty for failed orders or pending transactions.</param>
        /// <param name="storeName">The store name for the order. May be empty in case of unknown store context.</param>
        public OrderInfo(string receipt, string? transactionID, string storeName)
        {
            Receipt = CreateUnifiedReceipt(receipt, transactionID ?? "", storeName);
            TransactionID = transactionID ?? "";
            PurchasedProductInfo = new List<IPurchasedProductInfo>();
        }

        static string CreateUnifiedReceipt(string rawReceipt, string transactionId, string storeName)
        {
            return UnifiedReceiptFormatter.FormatUnifiedReceipt(rawReceipt, transactionId, storeName);
        }
    }
}
