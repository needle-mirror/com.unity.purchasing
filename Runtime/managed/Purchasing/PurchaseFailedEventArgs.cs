namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A purchase that failed including the product under purchase,
    /// the reason for the failure and any additional information.
    /// </summary>
    public class PurchaseFailedEventArgs
    {
        internal PurchaseFailedEventArgs(Product purchasedProduct, PurchaseFailureReason reason, string message)
        {
            this.purchasedProduct = purchasedProduct;
            this.reason = reason;
            this.message = message;
        }

        public Product purchasedProduct { get; private set; }
        public PurchaseFailureReason reason { get; private set; }
        public string message { get; private set; }
    }
}
