using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The model encapsulating additional information about an order.
    /// </summary>
    class OrderInfo : IOrderInfo
    {
        public IAppleOrderInfo Apple => this as IAppleOrderInfo;

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
                return CreateUnifiedReceipt(Apple.AppReceipt, TransactionID, Apple.StoreName);
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

        string m_Receipt;

        /// <summary>
        /// Parametrized constructor
        /// </summary>
        /// /// <param name="receipt">The receipt for the order.</param>
        /// /// <param name="transactionID">The transaction ID for the order.</param>
        /// /// <param name="storeName">The store name for the order.</param>
        public OrderInfo(string receipt, string transactionID, string storeName)
        {
            Receipt = CreateUnifiedReceipt(receipt, transactionID, storeName);
            TransactionID = transactionID;
        }

        static string CreateUnifiedReceipt(string rawReceipt, string transactionId, string storeName)
        {
            return UnifiedReceiptFormatter.FormatUnifiedReceipt(rawReceipt, transactionId, storeName);
        }
    }
}
