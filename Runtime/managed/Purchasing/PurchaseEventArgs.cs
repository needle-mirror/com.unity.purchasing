namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A purchase that succeeded, including the purchased product
    /// along with its purchase receipt.
    /// </summary>
    public class PurchaseEventArgs
    {
        internal PurchaseEventArgs(Product purchasedProduct)
        {
            this.purchasedProduct = purchasedProduct;
        }

        public Product purchasedProduct { get; private set; }
    }
}
