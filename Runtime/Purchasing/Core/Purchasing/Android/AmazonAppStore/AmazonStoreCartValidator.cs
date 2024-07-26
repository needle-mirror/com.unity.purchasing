namespace UnityEngine.Purchasing
{
    class AmazonStoreCartValidator : StoreCartValidator
    {
        internal AmazonStoreCartValidator()
            : base(AmazonApps.DisplayName, new SingleProductSingleQuantityCartValidator()) { }
    }
}
